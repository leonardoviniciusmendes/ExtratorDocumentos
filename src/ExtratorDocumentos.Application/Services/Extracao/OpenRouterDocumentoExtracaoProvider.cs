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

        public async Task<ExtracaoDocumentoResult?> ExtrairAsync(TipoDocumento tipo,
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
                    : new object[] { new { id = "response-healing" } }
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
                result.DadosBrutosJson = outputText;
            return result;
        }
    }
}
