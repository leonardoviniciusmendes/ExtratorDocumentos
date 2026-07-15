using System.Net.Http.Headers;
using System.Text.Json;
using ExtratorDocumentos.Application.Dtos.Documentos;
using Microsoft.Extensions.Configuration;

namespace ExtratorDocumentos.Application.Services.Extracao
{
    public sealed class OpenRouterContaService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;

        public OpenRouterContaService(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        public async Task<OpenRouterCreditosResponse> ObterCreditosAsync(
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "credits");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer", ObterApiKey());

            using var response = await _http.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"OpenRouter retornou {(int)response.StatusCode}: {responseJson}");

            using var doc = JsonDocument.Parse(responseJson);
            var data = doc.RootElement.TryGetProperty("data", out var dataElement)
                ? dataElement : doc.RootElement;
            var creditosTotais = ObterDecimal(data, "total_credits");
            var usoTotal = ObterDecimal(data, "total_usage");
            return new OpenRouterCreditosResponse(
                creditosTotais,
                usoTotal,
                creditosTotais.HasValue && usoTotal.HasValue
                    ? creditosTotais.Value - usoTotal.Value
                    : null);
        }

        private string ObterApiKey()
        {
            var apiKey = _configuration["DocumentExtraction:OpenRouter:ApiKey"]
                ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Configure DocumentExtraction__OpenRouter__ApiKey ou OPENROUTER_API_KEY.");
            return apiKey;
        }

        private static decimal? ObterDecimal(JsonElement item, string nome)
        {
            if (!item.TryGetProperty(nome, out var valor))
                return null;
            if (valor.ValueKind == JsonValueKind.Number &&
                valor.TryGetDecimal(out var numero))
                return numero;
            if (valor.ValueKind == JsonValueKind.String &&
                decimal.TryParse(valor.GetString(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out numero))
                return numero;
            return null;
        }
    }
}
