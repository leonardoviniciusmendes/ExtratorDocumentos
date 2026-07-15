using System.Security.Cryptography;
using System.Text.Json;
using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using ExtratorDocumentos.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public sealed class ProcessadorDocumentoIdentificacaoService
        : IProcessadorDocumentoIdentificacaoService
    {
        private readonly AppDbContext _db;
        private readonly LocalStorageService _storage;
        private readonly IArquivoAnaliseService _analiseService;
        private readonly IOpenRouterModelCandidateSelector _selector;
        private readonly IOpenRouterPipelineClient _openRouter;
        private readonly IAssinaturaEstruturalDocumentoService _assinaturaService;
        private readonly IIdentificadorTipoDocumentoService _identificadorTipo;
        private readonly ITipoDocumentoAprendizadoService _aprendizadoService;
        private readonly IAplicadorSchemaDocumentoService _aplicadorSchema;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProcessadorDocumentoIdentificacaoService> _logger;

        public ProcessadorDocumentoIdentificacaoService(AppDbContext db,
            LocalStorageService storage, IArquivoAnaliseService analiseService,
            IOpenRouterModelCandidateSelector selector,
            IOpenRouterPipelineClient openRouter,
            IAssinaturaEstruturalDocumentoService assinaturaService,
            IIdentificadorTipoDocumentoService identificadorTipo,
            ITipoDocumentoAprendizadoService aprendizadoService,
            IAplicadorSchemaDocumentoService aplicadorSchema,
            IConfiguration configuration,
            ILogger<ProcessadorDocumentoIdentificacaoService> logger)
        {
            _db = db;
            _storage = storage;
            _analiseService = analiseService;
            _selector = selector;
            _openRouter = openRouter;
            _assinaturaService = assinaturaService;
            _identificadorTipo = identificadorTipo;
            _aprendizadoService = aprendizadoService;
            _aplicadorSchema = aplicadorSchema;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ResultadoDocumentoPadronizadoDto> ProcessarAsync(
            string tipoDocumento, ArquivoDocumento arquivo,
            CancellationToken cancellationToken)
        {
            var tipoSolicitado = NormalizadorEstrutural.NormalizarCodigo(tipoDocumento);
            var bytes = await LerArquivoAsync(arquivo.Conteudo, cancellationToken);
            var hash = CalcularHash(bytes);
            var versaoSchema = _configuration["DocumentProcessing:VersaoSchemaIdentificacao"]
                ?? "identificacao-1.0";
            var versaoExtrator = _configuration["DocumentProcessing:VersaoExtrator"]
                ?? "1.0.0";

            var documento = await ObterOuCriarDocumentoAsync(
                arquivo, bytes, hash, versaoSchema, versaoExtrator,
                cancellationToken);
            var tipoSchema = await _db.TiposDocumento
                .Include(x => x.Schemas.Where(s => s.Status == StatusTipoDocumentoSchema.Ativo))
                .ThenInclude(x => x.Campos)
                .FirstOrDefaultAsync(x => x.Codigo == tipoSolicitado && x.Ativo,
                    cancellationToken);
            var schemaAtivo = tipoSchema?.Schemas.FirstOrDefault();
            if (tipoSchema == null || schemaAtivo == null)
                throw new InvalidOperationException(
                    $"SCHEMA_ATIVO_NAO_ENCONTRADO: Nao existe um schema ativo para o tipo {tipoSolicitado}.");
            schemaAtivo.TipoDocumento = tipoSchema;

            var extracaoSchemaAtivo = await BuscarExtracaoComSchemaAtivoAsync(
                documento.Id, tipoSolicitado, versaoExtrator, cancellationToken);
            if (extracaoSchemaAtivo is { Status: StatusExtracao.Concluido or StatusExtracao.ConcluidoComAlertas or StatusExtracao.RequerRevisao, ResultadoJson: not null })
                return DesserializarResultado(extracaoSchemaAtivo.ResultadoJson, true);
            if (extracaoSchemaAtivo is { Status: StatusExtracao.Processando or StatusExtracao.Pendente })
                throw new InvalidOperationException(
                    $"PROCESSAMENTO_EM_ANDAMENTO: Ja existe uma extracao em andamento para este arquivo e tipo. DocumentoExtracaoId={extracaoSchemaAtivo.Id}.");

            var extracao = extracaoSchemaAtivo ?? new DocumentoExtracao
            {
                DocumentoId = documento.Id,
                TipoDocumentoId = tipoSchema.Id,
                TipoDocumentoSchemaId = schemaAtivo.Id,
                TipoDocumentoSchema = schemaAtivo,
                TipoDocumentoSolicitado = tipoSolicitado,
                VersaoSchema = schemaAtivo.Versao,
                VersaoExtrator = versaoExtrator,
                Status = StatusExtracao.Pendente
            };
            if (extracaoSchemaAtivo == null)
                _db.DocumentoExtracoes.Add(extracao);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (EhViolacaoUnicidade(ex))
            {
                _db.ChangeTracker.Clear();
                var concorrente = await BuscarExtracaoComSchemaAtivoAsync(
                    documento.Id, tipoSolicitado, versaoExtrator, cancellationToken);
                if (concorrente?.ResultadoJson != null)
                    return DesserializarResultado(concorrente.ResultadoJson, true);
                if (concorrente != null)
                    throw new InvalidOperationException(
                        $"PROCESSAMENTO_EM_ANDAMENTO: Ja existe uma extracao em andamento para este arquivo e tipo. DocumentoExtracaoId={concorrente.Id}.");
                throw;
            }

            return await ExecutarExtracaoAsync(documento, extracao, tipoSolicitado,
                arquivo, bytes, hash, versaoSchema, versaoExtrator, cancellationToken);
        }

        private async Task<ResultadoDocumentoPadronizadoDto> ExecutarExtracaoAsync(
            Documento documento, DocumentoExtracao extracao, string tipoSolicitado,
            ArquivoDocumento arquivo, byte[] bytes, string hash, string versaoSchema,
            string versaoExtrator, CancellationToken cancellationToken)
        {
            extracao.Status = StatusExtracao.Processando;
            extracao.AtualizadoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                var caracteristicas = await _analiseService.AnalisarAsync(
                    arquivo.NomeOriginal, arquivo.MimeType, bytes, cancellationToken);
                var identificadoHipotese = new DocumentoIdentificado(
                    MapearTipoConhecido(tipoSolicitado), 0, "{}");
                var modelos = await _selector.SelecionarCandidatosParaExtracaoAsync(
                    identificadoHipotese, caracteristicas, cancellationToken);
                var chamada = await ExtrairComFallbackAsync(extracao, modelos,
                    tipoSolicitado, caracteristicas, bytes, cancellationToken);
                var resultado = chamada.Dados ??
                    throw new InvalidOperationException("DADOS_MINIMOS_NAO_ENCONTRADOS");

                CompletarResultado(resultado, documento, extracao, tipoSolicitado,
                    hash, versaoSchema, versaoExtrator, chamada.ModeloId);
                var padronizado = await _aplicadorSchema.AplicarAsync(
                    extracao.TipoDocumentoSchema!, resultado, cancellationToken);
                var tipoDocumento = extracao.TipoDocumentoSchema!.TipoDocumento;
                CompletarResultadoPadronizado(padronizado, documento, extracao,
                    tipoDocumento, extracao.TipoDocumentoSchema, true, 1m,
                    false, chamada.ModeloId, versaoExtrator);
                await _aprendizadoService.RegistrarCamposAdicionaisAsync(
                    tipoDocumento, padronizado.CamposAdicionais, cancellationToken);
                var status = DefinirStatus(resultado);
                if (padronizado.Validacao.RequerRevisaoHumana)
                    status = StatusExtracao.RequerRevisao;
                extracao.Status = status;
                extracao.TipoDocumentoId = tipoDocumento.Id;
                extracao.TipoDocumentoSchemaId = extracao.TipoDocumentoSchema.Id;
                extracao.SimilaridadeTipo = 1m;
                extracao.TipoReutilizado = true;
                extracao.TipoDocumentoIdentificado = resultado.Classificacao.TipoIdentificado;
                extracao.Confianca = resultado.Validacao.ConfiancaGeral;
                extracao.ModeloExtracao = chamada.ModeloId;
                extracao.ModeloIdentificacao = chamada.ModeloId;
                extracao.ResultadoJson = JsonSerializer.Serialize(padronizado);
                extracao.ProcessadoEm = DateTime.UtcNow;
                extracao.AtualizadoEm = DateTime.UtcNow;
                documento.Status = status == StatusExtracao.Concluido
                    ? StatusDocumento.Concluido
                    : StatusDocumento.EmAnalise;
                documento.ResultadoExtracaoJson = extracao.ResultadoJson;
                documento.TipoDocumentoIdentificado = MapearTipoConhecido(
                    resultado.Classificacao.TipoIdentificado ?? "outro");
                documento.ProcessadoEm = extracao.ProcessadoEm;
                documento.AtualizadoEm = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return padronizado;
            }
            catch (Exception ex)
            {
                extracao.Status = StatusExtracao.Erro;
                extracao.ErroCodigo = ExtrairCodigoErro(ex.Message);
                extracao.ErroMensagem = ex.Message;
                extracao.AtualizadoEm = DateTime.UtcNow;
                await _db.SaveChangesAsync(CancellationToken.None);
                _logger.LogError(ex,
                    "Falha na extracao de identificacao {DocumentoExtracaoId}", extracao.Id);
                return CriarRespostaErro(documento, extracao, hash, versaoSchema,
                    versaoExtrator);
            }
        }

        private async Task<ResultadoChamadaModelo<ResultadoIdentificacaoDocumentoDto>> ExtrairComFallbackAsync(
            DocumentoExtracao extracao, IReadOnlyList<ModeloSelecionado> modelos,
            string tipoSolicitado, CaracteristicasArquivo caracteristicas, byte[] bytes,
            CancellationToken cancellationToken)
        {
            ResultadoChamadaModelo<ResultadoIdentificacaoDocumentoDto>? ultimo = null;
            foreach (var modelo in modelos.Take(QuantidadeMaximaTentativas()))
            {
                ultimo = await _openRouter.ExtrairIdentificacaoUniversalAsync(
                    modelo, tipoSolicitado, caracteristicas, bytes,
                    extracao.TipoDocumentoSchema?.SchemaJson, cancellationToken);
                RegistrarUso(extracao, "extracao_identificacao", ultimo);
                if (ultimo.Sucesso && ultimo.Dados != null)
                    return ultimo;
            }

            throw new InvalidOperationException(
                ultimo?.Erro ?? "MODELOS_INDISPONIVEIS");
        }

        private async Task<Documento> ObterOuCriarDocumentoAsync(ArquivoDocumento arquivo,
            byte[] bytes, string hash, string versaoSchema, string versaoExtrator,
            CancellationToken cancellationToken)
        {
            var existente = await _db.Documentos
                .FirstOrDefaultAsync(x => x.HashSha256 == hash && !x.Excluido,
                    cancellationToken);
            if (existente != null) return existente;

            var caracteristicas = await _analiseService.AnalisarAsync(
                arquivo.NomeOriginal, arquivo.MimeType, bytes, cancellationToken);
            var documento = new Documento
            {
                NomeOriginal = Path.GetFileName(arquivo.NomeOriginal),
                HashSha256 = hash,
                Extensao = caracteristicas.Extensao,
                MimeType = caracteristicas.MimeType,
                TamanhoBytes = bytes.LongLength,
                Tipo = TipoDocumentoLegado.Outros,
                Status = StatusDocumento.Pendente,
                VersaoSchema = versaoSchema,
                VersaoExtrator = versaoExtrator
            };

            var chave = StorageKeyBuilder.CriarChaveOriginal(documento, 1, arquivo.NomeOriginal);
            var versao = new DocumentoVersao
            {
                DocumentoId = documento.Id,
                Versao = 1,
                NomeArquivo = Path.GetFileName(arquivo.NomeOriginal),
                TipoConteudo = documento.MimeType,
                TamanhoBytes = bytes.LongLength,
                HashSha256 = hash,
                ChaveStorage = chave
            };

            await using var stream = new MemoryStream(bytes);
            await _storage.SalvarAsync(chave, stream, cancellationToken);

            await _db.Documentos.AddAsync(documento, cancellationToken);
            await _db.DocumentoVersoes.AddAsync(versao, cancellationToken);
            try
            {
                RegistrarChangeTrackerDesenvolvimento();
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
            catch (DbUpdateException ex) when (EhViolacaoUnicidade(ex))
            {
                _db.Entry(documento).State = EntityState.Detached;
                _db.Entry(versao).State = EntityState.Detached;
                return await _db.Documentos
                    .FirstAsync(x => x.HashSha256 == hash && !x.Excluido,
                        cancellationToken);
            }

            return documento;
        }

        private void RegistrarChangeTrackerDesenvolvimento()
        {
            var ambiente = _configuration["ASPNETCORE_ENVIRONMENT"] ??
                _configuration["DOTNET_ENVIRONMENT"];
            if (!string.Equals(ambiente, "Development",
                    StringComparison.OrdinalIgnoreCase))
                return;

            var entries = _db.ChangeTracker.Entries()
                .Select(x => new
                {
                    Entidade = x.Metadata.ClrType.Name,
                    Estado = x.State.ToString(),
                    Chaves = x.Properties
                        .Where(p => p.Metadata.IsPrimaryKey())
                        .ToDictionary(
                            p => p.Metadata.Name,
                            p => p.CurrentValue?.ToString())
                })
                .ToList();

            _logger.LogDebug("ChangeTracker antes de salvar documento: {@Entries}",
                entries);
        }

        private Task<DocumentoExtracao?> BuscarExtracaoComSchemaAtivoAsync(
            Guid documentoId, string tipoSolicitado, string versaoExtrator,
            CancellationToken cancellationToken) =>
            _db.DocumentoExtracoes
                .Include(x => x.TipoDocumentoSchema)
                    .ThenInclude(x => x!.TipoDocumento)
                .Include(x => x.TipoDocumentoSchema)
                    .ThenInclude(x => x!.Campos)
                .FirstOrDefaultAsync(x =>
                    x.DocumentoId == documentoId &&
                    x.TipoDocumentoSolicitado == tipoSolicitado &&
                    x.VersaoExtrator == versaoExtrator &&
                    x.TipoDocumentoSchemaId != null &&
                    x.TipoDocumentoSchema != null &&
                    x.TipoDocumentoSchema.Ativo,
                    cancellationToken);

        private void RegistrarUso<T>(DocumentoExtracao extracao, string objetivo,
            ResultadoChamadaModelo<T> chamada)
        {
            _db.UsosOpenRouter.Add(new UsoOpenRouter
            {
                DocumentoId = extracao.DocumentoId,
                DocumentoExtracaoId = extracao.Id,
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

        private static void CompletarResultado(ResultadoIdentificacaoDocumentoDto resultado,
            Documento documento, DocumentoExtracao extracao, string tipoSolicitado,
            string hash, string versaoSchema, string versaoExtrator, string modelo)
        {
            resultado.DocumentoId = documento.Id;
            resultado.ResultadoReutilizado = false;
            resultado.Classificacao.TipoInformado = tipoSolicitado;
            resultado.Classificacao.TipoIdentificado =
                NormalizarTipo(resultado.Classificacao.TipoIdentificado ?? "outro");
            resultado.Classificacao.TipoConfirmado = string.Equals(
                tipoSolicitado, resultado.Classificacao.TipoIdentificado,
                StringComparison.OrdinalIgnoreCase);
            resultado.Validacao.TipoInformadoConfirmado =
                resultado.Classificacao.TipoConfirmado;
            if (!resultado.Classificacao.TipoConfirmado)
            {
                resultado.Validacao.Divergencias.Add(new DivergenciaDocumentoDto
                {
                    Campo = "tipoDocumento",
                    ValorInformado = tipoSolicitado,
                    ValorIdentificado = resultado.Classificacao.TipoIdentificado,
                    Mensagem = $"O arquivo enviado aparenta ser {resultado.Classificacao.TipoIdentificado}."
                });
                resultado.Validacao.Alertas.Add("TIPO_DOCUMENTO_DIVERGENTE");
            }
            resultado.Validacao.DocumentoVencido =
                DocumentoVencido(resultado.Identificacao.DataValidade);
            resultado.Validacao.DadosMinimosEncontrados =
                !string.IsNullOrWhiteSpace(resultado.Titular.NomeCompleto) &&
                !string.IsNullOrWhiteSpace(resultado.Identificacao.NumeroDocumento);
            if (!resultado.Validacao.DadosMinimosEncontrados)
                resultado.Validacao.CamposAusentes.Add("dados_minimos");
            resultado.Validacao.RequerRevisaoHumana =
                !resultado.Validacao.ArquivoLegivel ||
                !resultado.Validacao.DadosMinimosEncontrados ||
                resultado.Validacao.Divergencias.Count > 0 ||
                resultado.Validacao.ConfiancaGeral < 0.75m;
            resultado.CamposEspecificos.TipoDocumento =
                resultado.Classificacao.TipoIdentificado ?? tipoSolicitado;
            resultado.Processamento.HashSha256 = hash;
            resultado.Processamento.VersaoSchema = versaoSchema;
            resultado.Processamento.VersaoExtrator = versaoExtrator;
            resultado.Processamento.ModeloUtilizado = modelo;
            resultado.Processamento.ProcessadoEm = DateTime.UtcNow;
            var status = DefinirStatus(resultado);
            resultado.Status = status.ToString();
            extracao.Status = status;
        }

        private static void CompletarResultadoPadronizado(
            ResultadoDocumentoPadronizadoDto resultado,
            Documento documento,
            DocumentoExtracao extracao,
            TipoDocumento tipoDocumento,
            TipoDocumentoSchema schema,
            bool tipoReutilizado,
            decimal similaridade,
            bool requerRevisao,
            string modelo,
            string versaoExtrator)
        {
            resultado.DocumentoId = documento.Id;
            resultado.DocumentoExtracaoId = extracao.Id;
            resultado.ResultadoReutilizado = false;
            resultado.TipoDocumento.Id = tipoDocumento.Id;
            resultado.TipoDocumento.Codigo = tipoDocumento.Codigo;
            resultado.TipoDocumento.Nome = tipoDocumento.Nome;
            resultado.TipoDocumento.SchemaId = schema.Id;
            resultado.TipoDocumento.SchemaVersao = schema.Versao;
            resultado.TipoDocumento.Similaridade = similaridade;
            resultado.TipoDocumento.TipoReutilizado = tipoReutilizado;
            resultado.TipoDocumento.TipoConfirmado = tipoDocumento.Confirmado;
            resultado.Validacao.RequerRevisaoHumana =
                resultado.Validacao.RequerRevisaoHumana ||
                requerRevisao ||
                !tipoDocumento.Confirmado;
            if (requerRevisao)
                resultado.Validacao.Alertas.Add("TIPO_DOCUMENTO_REQUER_REVISAO");
            if (!tipoDocumento.Confirmado)
                resultado.Validacao.Alertas.Add("TIPO_DOCUMENTO_NAO_CONFIRMADO");
            resultado.Processamento.ModeloUtilizado = modelo;
            resultado.Processamento.VersaoExtrator = versaoExtrator;
            resultado.Processamento.ProcessadoEm = DateTime.UtcNow;
        }

        private static StatusExtracao DefinirStatus(ResultadoIdentificacaoDocumentoDto resultado)
        {
            if (!resultado.Validacao.ArquivoLegivel ||
                !resultado.Validacao.DadosMinimosEncontrados)
                return StatusExtracao.RequerRevisao;
            return resultado.Validacao.Divergencias.Count > 0 ||
                resultado.Validacao.Alertas.Count > 0
                    ? StatusExtracao.ConcluidoComAlertas
                    : StatusExtracao.Concluido;
        }

        private static ResultadoDocumentoPadronizadoDto DesserializarResultado(
            string json, bool reutilizado)
        {
            var result = JsonSerializer.Deserialize<ResultadoDocumentoPadronizadoDto>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            result.ResultadoReutilizado = reutilizado;
            return result;
        }

        private static ResultadoDocumentoPadronizadoDto CriarRespostaErro(
            Documento documento, DocumentoExtracao extracao, string hash,
            string versaoSchema, string versaoExtrator) =>
            new()
            {
                DocumentoId = documento.Id,
                DocumentoExtracaoId = extracao.Id,
                ResultadoReutilizado = false,
                Validacao =
                {
                    SchemaAplicado = false,
                    RequerRevisaoHumana = true,
                    Alertas = { extracao.ErroCodigo ?? "ERRO_PROCESSAMENTO" }
                },
                Processamento =
                {
                    VersaoExtrator = versaoExtrator
                }
            };

        private int QuantidadeMaximaTentativas()
        {
            if (!int.TryParse(_configuration["OpenRouter:Selecao:QuantidadeMaximaFallbacks"],
                    out var fallbacks))
                fallbacks = 2;
            return Math.Max(1, fallbacks + 1);
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

        private static string NormalizarTipo(string tipo) =>
            string.IsNullOrWhiteSpace(tipo)
                ? "outro"
                : tipo.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");

        private static string NormalizarTipo(TipoDocumentoProcessamento tipo) =>
            tipo switch
            {
                TipoDocumentoProcessamento.Identificacao => "identificacao",
                TipoDocumentoProcessamento.Endereco => "endereco",
                TipoDocumentoProcessamento.Contrato => "contrato",
                _ => "identificacao"
            };

        private static TipoDocumentoLegado MapearTipoConhecido(string tipo) =>
            NormalizarTipo(tipo) switch
            {
                "cnh" => TipoDocumentoLegado.CNH,
                "rg" => TipoDocumentoLegado.RG,
                "cpf" => TipoDocumentoLegado.CPF,
                "cin" => TipoDocumentoLegado.RG,
                "passaporte" => TipoDocumentoLegado.Outros,
                _ => TipoDocumentoLegado.Outros
            };

        private static bool? DocumentoVencido(string? dataValidade) =>
            DateTime.TryParse(dataValidade, out var data)
                ? data.Date < DateTime.UtcNow.Date
                : null;

        private static bool EhViolacaoUnicidade(DbUpdateException ex) =>
            ex.InnerException?.Message.Contains("Duplicate",
                StringComparison.OrdinalIgnoreCase) == true ||
            ex.InnerException?.Message.Contains("UNIQUE",
                StringComparison.OrdinalIgnoreCase) == true;

        private static string ExtrairCodigoErro(string erro)
        {
            var codigo = erro.Split(':')[0].Trim();
            return codigo.All(x => char.IsUpper(x) || x == '_')
                ? codigo
                : "ERRO_PROCESSAMENTO";
        }
    }
}
