using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using ExtratorDocumentos.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace ExtratorDocumentos.Application.Services.Extracao
{
    public sealed class DocumentoExtracaoService
    {
        private const decimal ConfiancaMinima = 80m;
        private readonly AppDbContext _db;
        private readonly LocalStorageService _storage;
        private readonly IEnumerable<IDocumentoExtracaoProvider> _providers;
        private readonly ILogger<DocumentoExtracaoService> _logger;
        private readonly IConfiguration _configuration;

        public DocumentoExtracaoService(AppDbContext db, LocalStorageService storage,
            IEnumerable<IDocumentoExtracaoProvider> providers, IConfiguration configuration,
            ILogger<DocumentoExtracaoService> logger)
        {
            _db = db; _storage = storage; _providers = providers;
            _configuration = configuration; _logger = logger;
        }

        public async Task<bool> ExtrairAsync(Guid documentoId, CancellationToken cancellationToken)
        {
            var documento = await _db.Documentos.Include(x => x.Versoes)
                .FirstOrDefaultAsync(x => x.Id == documentoId && !x.Excluido, cancellationToken);
            if (documento == null) return false;
            var versao = documento.Versoes.First(x => x.Versao == documento.VersaoAtual);
            documento.StatusExtracao = StatusExtracao.Processando;
            documento.ErroExtracao = null;
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                var nomeProvider = _configuration["DocumentExtraction:Provider"] ?? "OpenAI";
                var provider = _providers.FirstOrDefault(x =>
                    string.Equals(x.Nome, nomeProvider, StringComparison.OrdinalIgnoreCase)) ??
                    throw new InvalidOperationException(
                        $"Provedor de extracao '{nomeProvider}' nao registrado.");
                await using var stream = await _storage.AbrirLeituraAsync(versao.ChaveStorage, cancellationToken);
                var resultado = await provider.ExtrairAsync(documento.Tipo, versao.NomeArquivo,
                    versao.TipoConteudo, stream, cancellationToken) ??
                    throw new InvalidOperationException("O provedor nao retornou dados.");
                AplicarCpfExtraido(documento, resultado);
                await SalvarArtefatosAsync(
                    documento, versao, resultado, cancellationToken);

                if (EhDocumentoIdentificacao(documento.Tipo))
                {
                    var identificacao = await _db.IdentificacoesExtraidas
                        .FirstOrDefaultAsync(x => x.DocumentoId == documento.Id, cancellationToken);
                    identificacao ??= new IdentificacaoExtraida { DocumentoId = documento.Id };
                    MapearIdentificacao(identificacao, versao.Id, provider.Nome, resultado);
                    if (_db.Entry(identificacao).State == EntityState.Detached)
                        _db.IdentificacoesExtraidas.Add(identificacao);
                }

                if (PossuiEndereco(resultado))
                {
                    var endereco = await _db.EnderecosExtraidos.FirstOrDefaultAsync(
                        x => x.DocumentoId == documento.Id &&
                             x.DocumentoVersaoId == versao.Id, cancellationToken);
                    endereco ??= new EnderecoExtraido
                    {
                        DocumentoId = documento.Id,
                        DocumentoVersaoId = versao.Id
                    };
                    MapearEndereco(endereco, documento.Tipo, resultado);
                    if (_db.Entry(endereco).State == EntityState.Detached)
                        _db.EnderecosExtraidos.Add(endereco);
                }

                documento.StatusExtracao = resultado.Confianca >= ConfiancaMinima
                    ? StatusExtracao.Processado : StatusExtracao.RevisaoManual;
                documento.ExtraidoEm = DateTime.UtcNow;
                _db.DocumentoHistoricos.Add(new DocumentoHistorico
                {
                    DocumentoId = documento.Id,
                    Acao = "ExtracaoIdentificacao",
                    Observacao = $"Provedor: {provider.Nome}; Confianca: {resultado.Confianca}"
                });
                await _db.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                documento.StatusExtracao = StatusExtracao.Erro;
                documento.ErroExtracao = ex.Message;
                documento.ExtraidoEm = DateTime.UtcNow;
                await _db.SaveChangesAsync(CancellationToken.None);
                _logger.LogError(ex, "Falha ao extrair identificacao do documento {DocumentoId}", documento.Id);
                return false;
            }
        }

        private static void MapearIdentificacao(IdentificacaoExtraida x, Guid versaoId, string provedor,
            ExtracaoDocumentoResult r)
        {
            x.DocumentoVersaoId = versaoId; x.NomeCompleto = r.NomeCompleto; x.Cpf = SoDigitos(r.Cpf);
            x.Rg = r.Rg; x.OrgaoEmissor = r.OrgaoEmissor; x.UfEmissao = r.UfEmissao;
            x.DataNascimento = r.DataNascimento; x.Naturalidade = r.Naturalidade;
            x.Nacionalidade = r.Nacionalidade; x.NomeMae = r.NomeMae; x.NomePai = r.NomePai;
            x.NumeroCnh = SoDigitos(r.NumeroCnh); x.CategoriaCnh = r.CategoriaCnh;
            x.ValidadeCnh = r.ValidadeCnh; x.DataPrimeiraHabilitacao = r.DataPrimeiraHabilitacao;
            x.DataEmissao = r.DataEmissao; x.LocalEmissao = r.LocalEmissao;
            x.NumeroRenach = r.NumeroRenach; x.ObservacoesCnh = r.ObservacoesCnh;
            x.Cnpj = SoDigitos(r.Cnpj); x.RazaoSocial = r.RazaoSocial;
            x.NomeFantasia = r.NomeFantasia; x.EmissorDocumento = r.EmissorDocumento;
            x.NumeroCliente = r.NumeroCliente; x.NumeroInstalacao = r.NumeroInstalacao;
            x.MesReferencia = r.MesReferencia; x.DataVencimento = r.DataVencimento;
            x.MatriculaCertidao = r.MatriculaCertidao; x.Livro = r.Livro; x.Folha = r.Folha;
            x.Termo = r.Termo; x.Confianca = Math.Clamp(r.Confianca, 0, 100);
            x.Provedor = provedor; x.DadosBrutosJson = r.DadosBrutosJson; x.CriadoEm = DateTime.UtcNow;
        }

        private static void MapearEndereco(EnderecoExtraido x, TipoDocumento tipo,
            ExtracaoDocumentoResult r)
        {
            x.Cpf = SoDigitos(r.Cpf);
            x.Cnpj = SoDigitos(r.Cnpj);
            x.Cep = SoDigitos(r.Cep);
            x.Logradouro = r.Logradouro;
            x.Numero = r.NumeroEndereco;
            x.Complemento = r.Complemento;
            x.Bairro = r.Bairro;
            x.Cidade = r.Cidade;
            x.Uf = r.Estado;
            x.FonteDocumento = tipo.ToString();
            x.CriadoEm = DateTime.UtcNow;
        }

        private static bool EhDocumentoIdentificacao(TipoDocumento tipo) =>
            tipo is TipoDocumento.CNH or TipoDocumento.RG or TipoDocumento.CPF
                or TipoDocumento.CartaoCNPJ;

        private static bool PossuiEndereco(ExtracaoDocumentoResult r) =>
            !string.IsNullOrWhiteSpace(r.Cep) ||
            !string.IsNullOrWhiteSpace(r.Logradouro) ||
            !string.IsNullOrWhiteSpace(r.NumeroEndereco) ||
            !string.IsNullOrWhiteSpace(r.Complemento) ||
            !string.IsNullOrWhiteSpace(r.Bairro) ||
            !string.IsNullOrWhiteSpace(r.Cidade) ||
            !string.IsNullOrWhiteSpace(r.Estado);

        private static string? SoDigitos(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : new string(valor.Where(char.IsDigit).ToArray());

        private static void AplicarCpfExtraido(Documento documento, ExtracaoDocumentoResult resultado)
        {
            if (!string.IsNullOrWhiteSpace(documento.Cpf) ||
                documento.Papel != PapelDocumento.Titular)
                return;

            var cpfExtraido = SoDigitos(resultado.Cpf);
            if (cpfExtraido?.Length == 11)
                documento.Cpf = cpfExtraido;
        }

        private async Task SalvarArtefatosAsync(Documento documento, DocumentoVersao versao,
            ExtracaoDocumentoResult resultado, CancellationToken cancellationToken)
        {
            if (!StorageKeyBuilder.EhChaveEstruturada(versao.ChaveStorage))
            {
                var chaveAnterior = versao.ChaveStorage;
                var chaveNova = StorageKeyBuilder.CriarChaveOriginal(
                    documento, versao.Versao, versao.NomeArquivo);
                if (!string.Equals(chaveAnterior, chaveNova, StringComparison.OrdinalIgnoreCase))
                {
                    await using var originalLegado = await _storage.AbrirLeituraAsync(
                        chaveAnterior, cancellationToken);
                    await _storage.SalvarAsync(chaveNova, originalLegado, cancellationToken);
                }
                versao.ChaveStorage = chaveNova;
            }

            var chaveProcessado = StorageKeyBuilder.CriarChaveDerivada(
                versao.ChaveStorage, "processado", Path.GetExtension(versao.NomeArquivo));
            await using (var original = await _storage.AbrirLeituraAsync(
                versao.ChaveStorage, cancellationToken))
            {
                await _storage.SalvarAsync(chaveProcessado, original, cancellationToken);
            }

            var chaveJson = StorageKeyBuilder.CriarChaveDerivada(
                versao.ChaveStorage, "texto-extraido", ".json");
            await using (var json = new MemoryStream(
                Encoding.UTF8.GetBytes(resultado.DadosBrutosJson)))
            {
                await _storage.SalvarAsync(chaveJson, json, cancellationToken);
            }

            var chaveTexto = StorageKeyBuilder.CriarChaveDerivada(
                versao.ChaveStorage, "texto-extraido", ".txt");
            await using var texto = new MemoryStream(
                Encoding.UTF8.GetBytes(resultado.TextoExtraido ?? string.Empty));
            await _storage.SalvarAsync(chaveTexto, texto, cancellationToken);
        }
    }
}
