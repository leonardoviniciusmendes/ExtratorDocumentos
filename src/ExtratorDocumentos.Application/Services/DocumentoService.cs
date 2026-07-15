using System.Security.Cryptography;
using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using ExtratorDocumentos.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace ExtratorDocumentos.Application.Services
{
    public class DocumentoService
    {
        private readonly AppDbContext _db;
        private readonly LocalStorageService _storage;

        public DocumentoService(AppDbContext db, LocalStorageService storage)
        {
            _db = db;
            _storage = storage;
        }

        public async Task<Documento> CriarAsync(string? cpf, string? cpfDependente,
            string? cnpj, PapelDocumento papel, TipoParentesco tipoParentesco,
            TipoDocumento tipo, string? observacoes,
            string nomeArquivo, string tipoConteudo, Stream arquivo, CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(papel))
                throw new ArgumentException("Papel deve ser titular, dependente ou empresa.",
                    nameof(papel));
            var cpfNormalizado = NormalizarCpfOpcional(cpf);
            if ((EhDocumentoEndereco(tipo) || papel == PapelDocumento.Dependente) &&
                cpfNormalizado == null)
                throw new ArgumentException(
                    "CPF do titular e obrigatorio para upload de endereco ou dependente.",
                    nameof(cpf));

            var documento = new Documento
            {
                Cpf = cpfNormalizado,
                CpfDependente = papel == PapelDocumento.Dependente
                    ? NormalizarDocumentoOpcional(cpfDependente, 11, nameof(cpfDependente)) : null,
                Cnpj = papel == PapelDocumento.Empresa
                    ? NormalizarDocumento(cnpj, 14, nameof(cnpj)) : null,
                Papel = papel,
                TipoParentesco = tipoParentesco,
                Tipo = tipo,
                Observacoes = observacoes
            };
            _db.Documentos.Add(documento);
            await AdicionarVersaoInternaAsync(documento, nomeArquivo, tipoConteudo, arquivo, cancellationToken);
            _db.DocumentoHistoricos.Add(new DocumentoHistorico { DocumentoId = documento.Id, Acao = "Upload", StatusNovo = documento.Status, Observacao = observacoes });
            await _db.SaveChangesAsync(cancellationToken);
            return documento;
        }

        public async Task<DocumentoVersao?> AdicionarVersaoAsync(Guid id, string nomeArquivo,
            string tipoConteudo, Stream arquivo, string? observacao, string? usuario, CancellationToken cancellationToken)
        {
            var documento = await _db.Documentos.Include(x => x.Versoes)
                .FirstOrDefaultAsync(x => x.Id == id && !x.Excluido, cancellationToken);
            if (documento == null) return null;

            documento.VersaoAtual++;
            documento.Status = StatusDocumento.Pendente;
            documento.StatusExtracao = StatusExtracao.Pendente;
            documento.ErroExtracao = null;
            documento.AtualizadoEm = DateTime.UtcNow;
            var versao = await AdicionarVersaoInternaAsync(documento, nomeArquivo, tipoConteudo, arquivo, cancellationToken);
            _db.DocumentoHistoricos.Add(new DocumentoHistorico { DocumentoId = documento.Id, Acao = "NovaVersao", StatusNovo = documento.Status, Observacao = observacao, Usuario = usuario });
            await _db.SaveChangesAsync(cancellationToken);
            return versao;
        }

        public async Task<PagedResult<DocumentoResponse>> ListarAsync(DocumentoQuery filtros, CancellationToken cancellationToken)
        {
            var page = Math.Max(filtros.Page, 1);
            var pageSize = Math.Clamp(filtros.PageSize, 1, 200);
            var query = _db.Documentos.AsNoTracking().AsQueryable();
            if (!filtros.IncluirExcluidos) query = query.Where(x => !x.Excluido);
            if (!string.IsNullOrWhiteSpace(filtros.Cpf))
            {
                var cpf = NormalizarCpf(filtros.Cpf);
                query = query.Where(x => x.Cpf == cpf);
            }
            if (filtros.TipoParentesco.HasValue)
                query = query.Where(x => x.TipoParentesco == filtros.TipoParentesco);
            if (filtros.Tipo.HasValue) query = query.Where(x => x.Tipo == filtros.Tipo);
            if (filtros.Status.HasValue) query = query.Where(x => x.Status == filtros.Status);
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(x => x.CriadoEm).Skip((page - 1) * pageSize)
                .Take(pageSize).Select(x => ToResponse(x)).ToListAsync(cancellationToken);
            return new PagedResult<DocumentoResponse>(items, page, pageSize, total);
        }

        public Task<DocumentoResponse?> ObterAsync(Guid id, CancellationToken cancellationToken) =>
            _db.Documentos.AsNoTracking().Where(x => x.Id == id && !x.Excluido)
                .Select(x => ToResponse(x)).FirstOrDefaultAsync(cancellationToken);

        public async Task<(Stream Conteudo, DocumentoVersao Versao)?> DownloadAsync(Guid id, int? numeroVersao, CancellationToken cancellationToken)
        {
            var versao = await _db.DocumentoVersoes.AsNoTracking()
                .Where(x => x.DocumentoId == id && !x.Documento!.Excluido &&
                    (!numeroVersao.HasValue || x.Versao == numeroVersao))
                .OrderByDescending(x => x.Versao).FirstOrDefaultAsync(cancellationToken);
            if (versao == null || !await _storage.ExisteAsync(versao.ChaveStorage, cancellationToken)) return null;
            return (await _storage.AbrirLeituraAsync(versao.ChaveStorage, cancellationToken), versao);
        }

        public async Task<bool> AtualizarAsync(Guid id, AtualizarDocumentoRequest request, CancellationToken cancellationToken)
        {
            var documento = await _db.Documentos.FirstOrDefaultAsync(x => x.Id == id && !x.Excluido, cancellationToken);
            if (documento == null) return false;
            var anterior = documento.Status;
            if (request.Status.HasValue) documento.Status = request.Status.Value;
            documento.Observacoes = request.Observacoes;
            documento.AtualizadoEm = DateTime.UtcNow;
            _db.DocumentoHistoricos.Add(new DocumentoHistorico { DocumentoId = documento.Id, Acao = "Atualizacao", StatusAnterior = anterior, StatusNovo = documento.Status, Observacao = request.Observacoes, Usuario = request.Usuario });
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ExcluirAsync(Guid id, string? observacao, string? usuario, CancellationToken cancellationToken)
        {
            var documento = await _db.Documentos.FirstOrDefaultAsync(x => x.Id == id && !x.Excluido, cancellationToken);
            if (documento == null) return false;
            var anterior = documento.Status;
            documento.Excluido = true;
            documento.ExcluidoEm = DateTime.UtcNow;
            documento.AtualizadoEm = DateTime.UtcNow;
            documento.Status = StatusDocumento.Excluido;
            _db.DocumentoHistoricos.Add(new DocumentoHistorico { DocumentoId = documento.Id, Acao = "ExclusaoLogica", StatusAnterior = anterior, StatusNovo = documento.Status, Observacao = observacao, Usuario = usuario });
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<DocumentoVersao> AdicionarVersaoInternaAsync(Documento documento, string nomeArquivo,
            string tipoConteudo, Stream arquivo, CancellationToken cancellationToken)
        {
            await using var memoria = new MemoryStream();
            await arquivo.CopyToAsync(memoria, cancellationToken);
            var hash = Convert.ToHexString(SHA256.HashData(memoria.ToArray())).ToLowerInvariant();
            var chave = StorageKeyBuilder.CriarChaveOriginal(
                documento, documento.VersaoAtual, nomeArquivo);
            memoria.Position = 0;
            await _storage.SalvarAsync(chave, memoria, cancellationToken);
            var versao = new DocumentoVersao { Versao = documento.VersaoAtual, NomeArquivo = Path.GetFileName(nomeArquivo),
                TipoConteudo = string.IsNullOrWhiteSpace(tipoConteudo) ? "application/octet-stream" : tipoConteudo,
                TamanhoBytes = memoria.Length, HashSha256 = hash, ChaveStorage = chave };
            documento.Versoes.Add(versao);
            return versao;
        }

        private static DocumentoResponse ToResponse(Documento x) =>
            new(x.Id, x.Cpf, x.CpfDependente, x.Cnpj, x.Papel,
                x.TipoParentesco, x.Tipo, x.Status, x.Observacoes, x.VersaoAtual,
                x.Excluido, x.CriadoEm, x.AtualizadoEm, x.StatusExtracao,
                x.ErroExtracao, x.ExtraidoEm);

        private static string NormalizarCpf(string cpf)
        {
            var normalizado = new string((cpf ?? string.Empty).Where(char.IsDigit).ToArray());
            if (normalizado.Length != 11)
                throw new ArgumentException("CPF deve conter 11 digitos.", nameof(cpf));
            return normalizado;
        }

        private static string? NormalizarCpfOpcional(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf)) return null;
            return NormalizarCpf(cpf);
        }

        private static string NormalizarDocumento(string? valor, int tamanho, string campo)
        {
            var normalizado = new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
            if (normalizado.Length != tamanho)
                throw new ArgumentException(
                    $"{campo} deve conter {tamanho} digitos.", campo);
            return normalizado;
        }

        private static string? NormalizarDocumentoOpcional(string? valor, int tamanho, string campo)
        {
            if (string.IsNullOrWhiteSpace(valor) ||
                valor.Equals("null", StringComparison.OrdinalIgnoreCase) ||
                valor.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                return null;

            var normalizado = new string(valor.Where(char.IsDigit).ToArray());
            if (normalizado.Length == 0) return null;
            if (normalizado.Length != tamanho)
                throw new ArgumentException(
                    $"{campo} deve conter {tamanho} digitos.", campo);
            return normalizado;
        }

        private static bool EhDocumentoEndereco(TipoDocumento tipo) =>
            tipo is TipoDocumento.ComprovanteResidencia
                or TipoDocumento.ContaLuz
                or TipoDocumento.ContaAgua
                or TipoDocumento.ContaTelefone
                or TipoDocumento.ContaInternet
                or TipoDocumento.ContaGas
                or TipoDocumento.FaturaCartaoCredito
                or TipoDocumento.ExtratoBancario
                or TipoDocumento.ContratoLocacao
                or TipoDocumento.IPTU;
    }
}
