using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ExtratorDocumentos.Domain;
using Microsoft.Extensions.Configuration;

namespace ExtratorDocumentos.Application.Services.Extracao
{
    public sealed class OpenRouterDocumentoExtracaoProvider : IDocumentoExtracaoProvider
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        public string Nome => "OpenRouter";

        public OpenRouterDocumentoExtracaoProvider(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        public async Task<ExtracaoDocumentoResult?> ExtrairAsync(TipoDocumentoLegado tipo,
            string nomeArquivo, string tipoConteudo, Stream conteudo,
            CancellationToken cancellationToken)
        {
            var apiKey = _configuration["DocumentExtraction:OpenRouter:ApiKey"]
                ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Configure DocumentExtraction__OpenRouter__ApiKey ou OPENROUTER_API_KEY.");

            await using var memoria = new MemoryStream();
            await conteudo.CopyToAsync(memoria, cancellationToken);
            var dataUrl = $"data:{tipoConteudo};base64,{Convert.ToBase64String(memoria.ToArray())}";

            object arquivo = tipoConteudo.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                ? new { type = "image_url", image_url = new { url = dataUrl } }
                : new { type = "file", file = new { filename = nomeArquivo, file_data = dataUrl } };

            var payload = new
            {
                model = _configuration["DocumentExtraction:OpenRouter:Model"] ?? "google/gemini-2.5-flash",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = DocumentoExtracaoPrompt.Criar(tipo) },
                            arquivo
                        }
                    }
                },
                response_format = new { type = "json_object" },
                plugins = tipoConteudo.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
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

            using var response = await _http.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"OpenRouter retornou {(int)response.StatusCode}: {responseJson}");

            using var doc = JsonDocument.Parse(responseJson);
            var outputText = doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(outputText))
                return null;

            var result = JsonSerializer.Deserialize<ExtracaoDocumentoResult>(outputText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result != null)
            {
                var uso = ObterUso(doc.RootElement, payload.model);
                result.ProvedorUso = uso.Provedor;
                result.ModeloUso = uso.Modelo;
                result.RequisicaoIdUso = uso.RequisicaoId;
                result.TokensEntrada = uso.TokensEntrada;
                result.TokensSaida = uso.TokensSaida;
                result.TokensTotais = uso.TokensTotais;
                result.CustoUsd = uso.CustoUsd;
                result.DadosBrutosJson = AdicionarUsoAoJson(outputText, uso);
            }
            return result;
        }

        private static UsoOpenRouter ObterUso(JsonElement root, string modelo)
        {
            var usage = root.TryGetProperty("usage", out var usageElement)
                ? usageElement : default;
            return new UsoOpenRouter(
                "OpenRouter",
                root.TryGetProperty("model", out var modelElement) &&
                modelElement.ValueKind == JsonValueKind.String
                    ? modelElement.GetString()
                    : modelo,
                root.TryGetProperty("id", out var idElement) &&
                idElement.ValueKind == JsonValueKind.String
                    ? idElement.GetString()
                    : null,
                ObterInt(usage, "prompt_tokens"),
                ObterInt(usage, "completion_tokens"),
                ObterInt(usage, "total_tokens"),
                ObterDecimal(usage, "cost"));
        }

        private static string AdicionarUsoAoJson(string outputText, UsoOpenRouter uso)
        {
            using var doc = JsonDocument.Parse(outputText);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var property in doc.RootElement.EnumerateObject())
                    property.WriteTo(writer);

                writer.WritePropertyName("_uso");
                JsonSerializer.Serialize(writer, new
                {
                    provedor = uso.Provedor,
                    modelo = uso.Modelo,
                    requisicaoId = uso.RequisicaoId,
                    tokensEntrada = uso.TokensEntrada,
                    tokensSaida = uso.TokensSaida,
                    tokensTotais = uso.TokensTotais,
                    custoUsd = uso.CustoUsd
                });
                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static int? ObterInt(JsonElement item, string nome) =>
            item.ValueKind == JsonValueKind.Object &&
            item.TryGetProperty(nome, out var valor) &&
            valor.ValueKind == JsonValueKind.Number &&
            valor.TryGetInt32(out var numero)
                ? numero
                : null;

        private static decimal? ObterDecimal(JsonElement item, string nome) =>
            item.ValueKind == JsonValueKind.Object &&
            item.TryGetProperty(nome, out var valor) &&
            valor.ValueKind == JsonValueKind.Number &&
            valor.TryGetDecimal(out var numero)
                ? numero
                : null;

        private sealed record UsoOpenRouter(
            string Provedor,
            string? Modelo,
            string? RequisicaoId,
            int? TokensEntrada,
            int? TokensSaida,
            int? TokensTotais,
            decimal? CustoUsd);
    }
}
