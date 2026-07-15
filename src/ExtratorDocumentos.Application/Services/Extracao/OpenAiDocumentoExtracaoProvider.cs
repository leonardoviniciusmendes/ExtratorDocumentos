using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ExtratorDocumentos.Domain;
using Microsoft.Extensions.Configuration;

namespace ExtratorDocumentos.Application.Services.Extracao
{
    public sealed class OpenAiDocumentoExtracaoProvider : IDocumentoExtracaoProvider
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        public string Nome => "OpenAI";

        public OpenAiDocumentoExtracaoProvider(HttpClient http, IConfiguration configuration)
        {
            _http = http; _configuration = configuration;
        }

        public async Task<ExtracaoDocumentoResult?> ExtrairAsync(TipoDocumentoLegado tipo,
            string nomeArquivo, string tipoConteudo, Stream conteudo,
            CancellationToken cancellationToken)
        {
            var apiKey = _configuration["DocumentExtraction:OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("DocumentExtraction:OpenAI:ApiKey nao configurada.");

            await using var memoria = new MemoryStream();
            await conteudo.CopyToAsync(memoria, cancellationToken);
            var base64 = Convert.ToBase64String(memoria.ToArray());
            object conteudoArquivo = tipoConteudo.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                ? new { type = "input_image", image_url = $"data:{tipoConteudo};base64,{base64}", detail = "high" }
                : new { type = "input_file", filename = nomeArquivo, file_data = $"data:{tipoConteudo};base64,{base64}" };

            var payload = new
            {
                model = _configuration["DocumentExtraction:OpenAI:Model"] ?? "gpt-4.1-mini",
                input = new[] { new { role = "user", content = new object[]
                {
                    new { type = "input_text", text = DocumentoExtracaoPrompt.Criar(tipo) },
                    conteudoArquivo
                }}},
                text = new { format = new { type = "json_object" } }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var response = await _http.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"OpenAI retornou {(int)response.StatusCode}: {responseJson}");

            using var doc = JsonDocument.Parse(responseJson);
            var outputText = doc.RootElement.GetProperty("output").EnumerateArray()
                .Where(x => x.TryGetProperty("content", out _))
                .SelectMany(x => x.GetProperty("content").EnumerateArray())
                .First(x => x.GetProperty("type").GetString() == "output_text")
                .GetProperty("text").GetString();
            if (string.IsNullOrWhiteSpace(outputText)) return null;
            var result = JsonSerializer.Deserialize<ExtracaoDocumentoResult>(outputText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result != null) result.DadosBrutosJson = outputText;
            return result;
        }
    }
}
