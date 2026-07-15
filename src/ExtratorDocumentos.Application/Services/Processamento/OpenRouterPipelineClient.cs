using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Application.Services.Extracao;
using ExtratorDocumentos.Domain;
using Microsoft.Extensions.Configuration;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public sealed class OpenRouterPipelineClient : IOpenRouterPipelineClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;

        public OpenRouterPipelineClient(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        public async Task<ResultadoChamadaModelo<DocumentoIdentificado>> IdentificarAsync(
            ModeloSelecionado modelo, CaracteristicasArquivo caracteristicas,
            byte[] conteudo, CancellationToken cancellationToken)
        {
            var prompt = "Classifique o tipo do documento brasileiro. Retorne apenas JSON com os campos tipo, confianca e observacoes. " +
                "Tipos validos: RG, CPF, CNH, ComprovanteResidencia, ContaLuz, CertidaoNascimento, CertidaoCasamento, ContratoSocial, CartaoCNPJ, ContaAgua, ContaTelefone, ContaInternet, ContaGas, FaturaCartaoCredito, ExtratoBancario, ContratoLocacao, IPTU, Elegibilidade, FichaAssociativa, DocumentoOficialComSelfie, Outros.";
            var chamada = await EnviarAsync(modelo, prompt, caracteristicas, conteudo,
                cancellationToken);
            if (!chamada.Sucesso || string.IsNullOrWhiteSpace(chamada.Conteudo))
                return chamada.ToResultado<DocumentoIdentificado>(null);

            try
            {
                using var doc = JsonDocument.Parse(chamada.Conteudo);
                var tipoTexto = doc.RootElement.TryGetProperty("tipo", out var tipoElement)
                    ? tipoElement.GetString() : "Outros";
                var confianca = doc.RootElement.TryGetProperty("confianca", out var confElement) &&
                    confElement.TryGetDecimal(out var valor) ? valor : 0m;
                if (!Enum.TryParse<TipoDocumento>(tipoTexto, true, out var tipo))
                    tipo = TipoDocumento.Outros;
                return chamada.ToResultado(new DocumentoIdentificado(
                    tipo, Math.Clamp(confianca, 0, 100), chamada.Conteudo));
            }
            catch (JsonException ex)
            {
                var invalida = chamada with
                {
                    Sucesso = false,
                    Erro = $"JSON de identificacao invalido: {ex.Message}"
                };
                return invalida.ToResultado<DocumentoIdentificado>(null);
            }
        }

        public async Task<ResultadoChamadaModelo<ExtracaoDocumentoResult>> ExtrairAsync(
            ModeloSelecionado modelo, DocumentoIdentificado documento,
            CaracteristicasArquivo caracteristicas, byte[] conteudo,
            CancellationToken cancellationToken)
        {
            var chamada = await EnviarAsync(modelo,
                DocumentoExtracaoPrompt.Criar(documento.Tipo), caracteristicas, conteudo,
                cancellationToken);
            if (!chamada.Sucesso || string.IsNullOrWhiteSpace(chamada.Conteudo))
                return chamada.ToResultado<ExtracaoDocumentoResult>(null);

            try
            {
                var result = JsonSerializer.Deserialize<ExtracaoDocumentoResult>(
                    chamada.Conteudo,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result == null)
                    return new ResultadoChamadaModelo<ExtracaoDocumentoResult>(
                        null, chamada.ModeloId, chamada.TokensEntrada,
                        chamada.TokensSaida, chamada.Custo, chamada.DuracaoMs,
                        false, "JSON de extracao vazio.", chamada.RequestId);

                result.DadosBrutosJson = chamada.Conteudo;
                return chamada.ToResultado(result);
            }
            catch (JsonException ex)
            {
                var invalida = chamada with
                {
                    Sucesso = false,
                    Erro = $"JSON de extracao invalido: {ex.Message}"
                };
                return invalida.ToResultado<ExtracaoDocumentoResult>(null);
            }
        }

        public async Task<ResultadoChamadaModelo<ResultadoIdentificacaoDocumentoDto>> ExtrairIdentificacaoUniversalAsync(
            ModeloSelecionado modelo, string tipoDocumentoSolicitado,
            CaracteristicasArquivo caracteristicas, byte[] conteudo,
            CancellationToken cancellationToken)
        {
            var chamada = await EnviarAsync(modelo,
                DocumentoIdentificacaoPrompt.Criar(tipoDocumentoSolicitado),
                caracteristicas, conteudo, cancellationToken);
            if (!chamada.Sucesso || string.IsNullOrWhiteSpace(chamada.Conteudo))
                return chamada.ToResultado<ResultadoIdentificacaoDocumentoDto>(null);

            try
            {
                var result = JsonSerializer.Deserialize<ResultadoIdentificacaoDocumentoDto>(
                    chamada.Conteudo,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result == null)
                    return new ResultadoChamadaModelo<ResultadoIdentificacaoDocumentoDto>(
                        null, chamada.ModeloId, chamada.TokensEntrada,
                        chamada.TokensSaida, chamada.Custo, chamada.DuracaoMs,
                        false, "JSON de identificacao universal vazio.",
                        chamada.RequestId);

                GarantirBlocos(result);
                return chamada.ToResultado(result);
            }
            catch (JsonException ex)
            {
                var invalida = chamada with
                {
                    Sucesso = false,
                    Erro = $"JSON_OPENROUTER_INVALIDO: {ex.Message}"
                };
                return invalida.ToResultado<ResultadoIdentificacaoDocumentoDto>(null);
            }
        }

        private async Task<ChamadaOpenRouter> EnviarAsync(ModeloSelecionado modelo,
            string prompt, CaracteristicasArquivo caracteristicas, byte[] conteudo,
            CancellationToken cancellationToken)
        {
            var apiKey = _configuration["DocumentExtraction:OpenRouter:ApiKey"]
                ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Configure DocumentExtraction__OpenRouter__ApiKey ou OPENROUTER_API_KEY.");

            var dataUrl = $"data:{caracteristicas.MimeType};base64,{Convert.ToBase64String(conteudo)}";
            object arquivo = caracteristicas.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                ? new { type = "image_url", image_url = new { url = dataUrl } }
                : new { type = "file", file = new { filename = caracteristicas.NomeOriginal, file_data = dataUrl } };
            var payload = new
            {
                model = modelo.ModeloId,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = prompt },
                            arquivo
                        }
                    }
                },
                response_format = new { type = "json_object" },
                plugins = caracteristicas.MimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                    ? new object[]
                    {
                        new { id = "file-parser", pdf = new { engine = "mistral-ocr" } },
                        new { id = "response-healing" }
                    }
                    : new object[] { new { id = "response-healing" } },
                usage = new { include = true }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.TryAddWithoutValidation("HTTP-Referer",
                _configuration["DocumentExtraction:OpenRouter:SiteUrl"] ?? "https://localhost");
            request.Headers.TryAddWithoutValidation("X-Title", "ExtratorDocumentos");
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var sw = Stopwatch.StartNew();
            using var response = await _http.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            sw.Stop();
            if (!response.IsSuccessStatusCode)
                return new ChamadaOpenRouter(modelo.ModeloId, null, null, null, null,
                    sw.ElapsedMilliseconds, false,
                    $"OpenRouter retornou {(int)response.StatusCode}: {responseJson}", null);

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            var outputText = root.GetProperty("choices")[0]
                .GetProperty("message").GetProperty("content").GetString();
            var usage = root.TryGetProperty("usage", out var usageElement)
                ? usageElement : default;
            return new ChamadaOpenRouter(modelo.ModeloId, outputText,
                ObterInt(usage, "prompt_tokens"),
                ObterInt(usage, "completion_tokens"),
                ObterDecimal(usage, "cost"),
                sw.ElapsedMilliseconds,
                !string.IsNullOrWhiteSpace(outputText),
                null,
                root.TryGetProperty("id", out var idElement) ? idElement.GetString() : null);
        }

        private static int? ObterInt(JsonElement item, string nome) =>
            item.ValueKind == JsonValueKind.Object &&
            item.TryGetProperty(nome, out var valor) &&
            valor.TryGetInt32(out var numero) ? numero : null;

        private static decimal? ObterDecimal(JsonElement item, string nome) =>
            item.ValueKind == JsonValueKind.Object &&
            item.TryGetProperty(nome, out var valor) &&
            valor.TryGetDecimal(out var numero) ? numero : null;

        private static void GarantirBlocos(ResultadoIdentificacaoDocumentoDto result)
        {
            result.Classificacao ??= new ClassificacaoDocumentoDto();
            result.Titular ??= new TitularDocumentoDto();
            result.Identificacao ??= new IdentificacaoDocumentoDadosDto();
            result.CamposEspecificos ??= new CamposEspecificosDocumentoDto();
            result.CamposExtraidos ??= new List<CampoExtraidoDocumentoDto>();
            result.Validacao ??= new ValidacaoDocumentoDto();
            result.Processamento ??= new ProcessamentoIdentificacaoDto();
        }

        private sealed record ChamadaOpenRouter(
            string ModeloId,
            string? Conteudo,
            int? TokensEntrada,
            int? TokensSaida,
            decimal? Custo,
            long DuracaoMs,
            bool Sucesso,
            string? Erro,
            string? RequestId)
        {
            public ResultadoChamadaModelo<T> ToResultado<T>(T? dados) =>
                new(dados, ModeloId, TokensEntrada, TokensSaida, Custo,
                    DuracaoMs, Sucesso, Erro, RequestId);
        }
    }
}
