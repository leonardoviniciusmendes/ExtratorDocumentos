using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using ExtratorDocumentos.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

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

                var identificacao = await _db.IdentificacoesExtraidas
                    .FirstOrDefaultAsync(x => x.DocumentoId == documento.Id, cancellationToken);
                identificacao ??= new IdentificacaoExtraida { DocumentoId = documento.Id };
                Mapear(identificacao, versao.Id, provider.Nome, resultado);
                if (_db.Entry(identificacao).State == EntityState.Detached)
                    _db.IdentificacoesExtraidas.Add(identificacao);

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

        private static void Mapear(IdentificacaoExtraida x, Guid versaoId, string provedor,
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
            x.NomeFantasia = r.NomeFantasia; x.Endereco = r.Endereco;
            x.NomeTitularEndereco = r.NomeTitularEndereco; x.Logradouro = r.Logradouro;
            x.NumeroEndereco = r.NumeroEndereco; x.Complemento = r.Complemento;
            x.Bairro = r.Bairro; x.Cidade = r.Cidade; x.Estado = r.Estado;
            x.Cep = SoDigitos(r.Cep); x.EmissorDocumento = r.EmissorDocumento;
            x.NumeroCliente = r.NumeroCliente; x.NumeroInstalacao = r.NumeroInstalacao;
            x.MesReferencia = r.MesReferencia; x.DataVencimento = r.DataVencimento;
            x.MatriculaCertidao = r.MatriculaCertidao; x.Livro = r.Livro; x.Folha = r.Folha;
            x.Termo = r.Termo; x.Confianca = Math.Clamp(r.Confianca, 0, 100);
            x.Provedor = provedor; x.DadosBrutosJson = r.DadosBrutosJson; x.CriadoEm = DateTime.UtcNow;
        }

        private static string? SoDigitos(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : new string(valor.Where(char.IsDigit).ToArray());
    }
}
