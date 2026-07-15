using System.Security.Cryptography;
using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Application.Services.Extracao;
using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using ExtratorDocumentos.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public sealed class ProcessadorDocumentoService : IProcessadorDocumentoService
    {
        private readonly AppDbContext _db;
        private readonly LocalStorageService _storage;
        private readonly IArquivoAnaliseService _analiseService;
        private readonly IOpenRouterModelCandidateSelector _selector;
        private readonly IOpenRouterPipelineClient _openRouter;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProcessadorDocumentoService> _logger;

        public ProcessadorDocumentoService(AppDbContext db, LocalStorageService storage,
            IArquivoAnaliseService analiseService,
            IOpenRouterModelCandidateSelector candidateSelector,
            IOpenRouterPipelineClient openRouter, IConfiguration configuration,
            ILogger<ProcessadorDocumentoService> logger)
        {
            _db = db;
            _storage = storage;
            _analiseService = analiseService;
            _selector = candidateSelector;
            _openRouter = openRouter;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ResultadoProcessamentoDocumento> ProcessarAsync(
            ArquivoDocumento arquivo, CancellationToken cancellationToken)
        {
            ValidarArquivo(arquivo);
            var bytes = await LerArquivoAsync(arquivo.Conteudo, cancellationToken);
            var hash = CalcularHash(bytes);
            var versaoExtrator = _configuration["DocumentProcessing:VersaoExtrator"] ?? "1";
            var versaoSchema = _configuration["DocumentProcessing:VersaoSchema"] ?? "1";

            var existente = await BuscarPorHashAsync(hash, cancellationToken);
            if (PodeReaproveitar(existente, versaoExtrator, versaoSchema))
                return ToResultado(existente!, true, false);
            if (existente is { Status: StatusDocumento.Processando or StatusDocumento.Pendente })
                return ToResultado(existente, true, false);

            var caracteristicas = await _analiseService.AnalisarAsync(
                arquivo.NomeOriginal, arquivo.MimeType, bytes, cancellationToken);
            var documento = CriarDocumento(arquivo, caracteristicas, hash,
                versaoExtrator, versaoSchema);

            try
            {
                _db.Documentos.Add(documento);
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (EhViolacaoUnicidade(ex))
            {
                _db.ChangeTracker.Clear();
                var duplicado = await BuscarPorHashAsync(hash, cancellationToken);
                if (duplicado != null)
                    return ToResultado(duplicado, true, false);
                throw;
            }

            await SalvarOriginalAsync(documento, arquivo.NomeOriginal, bytes, cancellationToken);
            await ProcessarNovoDocumentoAsync(documento, caracteristicas, bytes,
                cancellationToken);

            return ToResultado(documento, false, true);
        }

        private async Task ProcessarNovoDocumentoAsync(Documento documento,
            CaracteristicasArquivo caracteristicas, byte[] bytes,
            CancellationToken cancellationToken)
        {
            documento.Status = StatusDocumento.Processando;
            documento.StatusExtracao = StatusExtracao.Processando;
            documento.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                var modelosIdentificacao = await _selector
                    .SelecionarCandidatosParaIdentificacaoAsync(caracteristicas, cancellationToken);
                var identificacao = await IdentificarComFallbackAsync(
                    documento, modelosIdentificacao, caracteristicas, bytes,
                    cancellationToken);
                var documentoIdentificado = identificacao.Dados!;

                documento.TipoDocumentoIdentificado = documentoIdentificado.Tipo;
                documento.Tipo = documentoIdentificado.Tipo;
                documento.ConfiancaIdentificacao = documentoIdentificado.Confianca;
                documento.ResultadoIdentificacaoJson = documentoIdentificado.ResultadoJson;
                documento.ModeloIdentificacao = identificacao.ModeloId;

                var modelosExtracao = await _selector.SelecionarCandidatosParaExtracaoAsync(
                    documentoIdentificado, caracteristicas, cancellationToken);
                var extracao = await ExtrairComFallbackAsync(documento,
                    modelosExtracao, documentoIdentificado, caracteristicas, bytes,
                    cancellationToken);
                var dadosExtraidos = extracao.Dados!;

                documento.ResultadoExtracaoJson = dadosExtraidos.DadosBrutosJson;
                documento.ModeloExtracao = extracao.ModeloId;
                documento.Status = StatusDocumento.Concluido;
                documento.StatusExtracao = StatusExtracao.Processado;
                documento.ProcessadoEm = DateTime.UtcNow;
                documento.ExtraidoEm = documento.ProcessadoEm;
                documento.AtualizadoEm = DateTime.UtcNow;
                documento.ErroProcessamento = null;
                documento.ErroExtracao = null;
                _db.DocumentoHistoricos.Add(new DocumentoHistorico
                {
                    DocumentoId = documento.Id,
                    Acao = "PipelineConcluido",
                    StatusNovo = documento.Status
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                documento.Status = StatusDocumento.Erro;
                documento.StatusExtracao = StatusExtracao.Erro;
                documento.ErroProcessamento = ex.Message;
                documento.ErroExtracao = ex.Message;
                documento.AtualizadoEm = DateTime.UtcNow;
                _db.DocumentoHistoricos.Add(new DocumentoHistorico
                {
                    DocumentoId = documento.Id,
                    Acao = "PipelineErro",
                    StatusNovo = documento.Status,
                    Observacao = ex.Message
                });
                await _db.SaveChangesAsync(CancellationToken.None);
                _logger.LogError(ex,
                    "Falha no pipeline do documento {DocumentoId}", documento.Id);
            }
        }

        private async Task SalvarOriginalAsync(Documento documento, string nomeOriginal,
            byte[] bytes, CancellationToken cancellationToken)
        {
            await using var stream = new MemoryStream(bytes);
            var chave = StorageKeyBuilder.CriarChaveOriginal(documento, 1, nomeOriginal);
            await _storage.SalvarAsync(chave, stream, cancellationToken);
            documento.Versoes.Add(new DocumentoVersao
            {
                DocumentoId = documento.Id,
                Versao = 1,
                NomeArquivo = Path.GetFileName(nomeOriginal),
                TipoConteudo = documento.MimeType,
                TamanhoBytes = bytes.LongLength,
                HashSha256 = documento.HashSha256,
                ChaveStorage = chave
            });
            _db.DocumentoHistoricos.Add(new DocumentoHistorico
            {
                DocumentoId = documento.Id,
                Acao = "UploadPipeline",
                StatusNovo = documento.Status
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<ResultadoChamadaModelo<DocumentoIdentificado>> IdentificarComFallbackAsync(
            Documento documento, IReadOnlyList<ModeloSelecionado> modelos,
            CaracteristicasArquivo caracteristicas, byte[] bytes,
            CancellationToken cancellationToken)
        {
            ResultadoChamadaModelo<DocumentoIdentificado>? ultimo = null;
            foreach (var modelo in modelos.Take(QuantidadeMaximaTentativas()))
            {
                ultimo = await _openRouter.IdentificarAsync(
                    modelo, caracteristicas, bytes, cancellationToken);
                RegistrarUso(documento, "identificacao", ultimo);
                if (ultimo.Sucesso && ultimo.Dados != null)
                    return ultimo;
            }

            throw new InvalidOperationException(
                ultimo?.Erro ?? "Falha ao identificar documento.");
        }

        private async Task<ResultadoChamadaModelo<ExtracaoDocumentoResult>> ExtrairComFallbackAsync(
            Documento documento, IReadOnlyList<ModeloSelecionado> modelos,
            DocumentoIdentificado identificado, CaracteristicasArquivo caracteristicas,
            byte[] bytes, CancellationToken cancellationToken)
        {
            ResultadoChamadaModelo<ExtracaoDocumentoResult>? ultimo = null;
            foreach (var modelo in modelos.Take(QuantidadeMaximaTentativas()))
            {
                ultimo = await _openRouter.ExtrairAsync(
                    modelo, identificado, caracteristicas, bytes, cancellationToken);
                RegistrarUso(documento, "extracao", ultimo);
                if (ultimo.Sucesso && ultimo.Dados != null)
                    return ultimo;
            }

            throw new InvalidOperationException(
                ultimo?.Erro ?? "Falha ao extrair dados do documento.");
        }

        private int QuantidadeMaximaTentativas()
        {
            if (!int.TryParse(_configuration["OpenRouter:Selecao:QuantidadeMaximaFallbacks"],
                    out var fallbacks))
                fallbacks = 2;
            return Math.Max(1, fallbacks + 1);
        }

        private void RegistrarUso<T>(Documento documento, string objetivo,
            ResultadoChamadaModelo<T> chamada)
        {
            _db.UsosOpenRouter.Add(new UsoOpenRouter
            {
                DocumentoId = documento.Id,
                ModeloId = chamada.ModeloId,
                Objetivo = objetivo,
                TokensEntrada = chamada.TokensEntrada,
                TokensSaida = chamada.TokensSaida,
                Custo = chamada.Custo,
                DuracaoMs = chamada.DuracaoMs,
                Sucesso = chamada.Sucesso,
                Erro = chamada.Erro,
                RequestId = chamada.RequestId
            });
        }

        private static Documento CriarDocumento(ArquivoDocumento arquivo,
            CaracteristicasArquivo caracteristicas, string hash, string versaoExtrator,
            string versaoSchema) =>
            new()
            {
                NomeOriginal = Path.GetFileName(arquivo.NomeOriginal),
                HashSha256 = hash,
                Extensao = caracteristicas.Extensao,
                MimeType = caracteristicas.MimeType,
                TamanhoBytes = caracteristicas.TamanhoBytes,
                Tipo = TipoDocumento.Outros,
                Status = StatusDocumento.Pendente,
                StatusExtracao = StatusExtracao.Pendente,
                VersaoExtrator = versaoExtrator,
                VersaoSchema = versaoSchema
            };

        private Task<Documento?> BuscarPorHashAsync(string hash,
            CancellationToken cancellationToken) =>
            _db.Documentos.Include(x => x.Versoes)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.HashSha256 == hash && !x.Excluido,
                    cancellationToken);

        private static bool PodeReaproveitar(Documento? documento,
            string versaoExtrator, string versaoSchema) =>
            documento is
            {
                Status: StatusDocumento.Concluido,
                ResultadoExtracaoJson: not null
            } &&
            documento.VersaoExtrator == versaoExtrator &&
            documento.VersaoSchema == versaoSchema;

        private static ResultadoProcessamentoDocumento ToResultado(
            Documento documento, bool reutilizado, bool executado) =>
            new(documento.Id, documento.Status, reutilizado, executado,
                documento.TipoDocumentoIdentificado ?? documento.Tipo,
                documento.ModeloIdentificacao, documento.ModeloExtracao,
                documento.ResultadoExtracaoJson, documento.ErroProcessamento);

        private static void ValidarArquivo(ArquivoDocumento arquivo)
        {
            if (string.IsNullOrWhiteSpace(arquivo.NomeOriginal))
                throw new ArgumentException("Nome do arquivo e obrigatorio.");
            if (arquivo.Conteudo == null || !arquivo.Conteudo.CanRead)
                throw new ArgumentException("Arquivo invalido.");
        }

        private static async Task<byte[]> LerArquivoAsync(Stream stream,
            CancellationToken cancellationToken)
        {
            await using var memoria = new MemoryStream();
            await stream.CopyToAsync(memoria, cancellationToken);
            if (memoria.Length == 0)
                throw new ArgumentException("Arquivo vazio.");
            return memoria.ToArray();
        }

        private static string CalcularHash(byte[] bytes) =>
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        private static bool EhViolacaoUnicidade(DbUpdateException ex) =>
            ex.InnerException?.Message.Contains("Duplicate",
                StringComparison.OrdinalIgnoreCase) == true ||
            ex.InnerException?.Message.Contains("UNIQUE",
                StringComparison.OrdinalIgnoreCase) == true;
    }
}
