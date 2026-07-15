using System.Net.Http.Headers;
using System.Text.Json;
using ExtratorDocumentos.Application.Dtos.Documentos;
using Microsoft.Extensions.Configuration;

namespace ExtratorDocumentos.Application.Services.Extracao
{
    public sealed class OpenRouterModelosService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;

        public OpenRouterModelosService(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        public async Task<IReadOnlyList<OpenRouterModeloResponse>> ListarAsync(
            bool apenasCompativeis, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "models");
            var apiKey = _configuration["DocumentExtraction:OpenRouter:ApiKey"]
                ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await _http.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"OpenRouter retornou {(int)response.StatusCode}: {responseJson}");

            using var doc = JsonDocument.Parse(responseJson);
            var modelos = new List<OpenRouterModeloResponse>();
            foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                var inputModalities = ObterArrayString(item, "architecture", "input_modalities");
                var outputModalities = ObterArrayString(item, "architecture", "output_modalities");
                var supportedParameters = ObterArrayString(item, "supported_parameters");
                var aceitaArquivo = inputModalities.Contains("file");
                var aceitaImagem = inputModalities.Contains("image");
                var saidaTexto = outputModalities.Count == 0 || outputModalities.Contains("text");
                var suportaJson = supportedParameters.Contains("response_format");
                var suportaStructuredOutputs = supportedParameters.Contains("structured_outputs");
                var compativel = (aceitaArquivo || aceitaImagem) && saidaTexto && suportaJson;

                if (apenasCompativeis && !compativel)
                    continue;

                var id = ObterString(item, "id") ?? string.Empty;
                modelos.Add(new OpenRouterModeloResponse(
                    id,
                    ObterString(item, "name") ?? id,
                    DefinirObjetivo(id, aceitaArquivo, aceitaImagem, suportaStructuredOutputs),
                    ObterInt(item, "context_length"),
                    aceitaArquivo,
                    aceitaImagem,
                    saidaTexto,
                    suportaJson,
                    suportaStructuredOutputs,
                    ObterPrecoPorMilhaoTokens(item, "prompt"),
                    ObterPrecoPorMilhaoTokens(item, "completion")));
            }

            return modelos
                .OrderByDescending(x => x.AceitaArquivo)
                .ThenByDescending(x => x.AceitaImagem)
                .ThenByDescending(x => x.SuportaStructuredOutputs)
                .ThenBy(x => x.PrecoEntradaPorMilhaoTokens ?? decimal.MaxValue)
                .ThenBy(x => x.Id)
                .ToList();
        }

        private static string DefinirObjetivo(string id, bool aceitaArquivo,
            bool aceitaImagem, bool suportaStructuredOutputs)
        {
            var modelo = id.ToLowerInvariant();
            if (modelo.Contains("gemini"))
                return "Padrao recomendado para extracao de documentos: bom suporte multimodal, custo controlado e baixa latencia.";
            if (modelo.Contains("claude"))
                return "Analise documental mais criteriosa quando precisao e interpretacao de campos ambiguos forem prioridade.";
            if (modelo.Contains("gpt") || modelo.Contains("openai"))
                return suportaStructuredOutputs
                    ? "Extracao com JSON estruturado e validacao mais previsivel para respostas de producao."
                    : "Extracao generalista com boa aderencia a instrucoes e formato JSON.";
            if (modelo.Contains("mistral"))
                return "Boa opcao para documentos e OCR; util como alternativa ao fluxo com file-parser.";
            if (modelo.Contains("qwen") || modelo.Contains("deepseek") || modelo.Contains("llama"))
                return "Opcao economica para texto ja extraido; usar com cautela para imagem ou PDF complexo.";
            if (aceitaArquivo || aceitaImagem)
                return "Modelo multimodal elegivel para extracao de documentos no OpenRouter.";
            return "Modelo de texto; indicado apenas depois que OCR ou parser ja transformou o documento em texto.";
        }

        private static IReadOnlySet<string> ObterArrayString(JsonElement item, params string[] caminho)
        {
            if (!TentarObter(item, out var atual, caminho) ||
                atual.ValueKind != JsonValueKind.Array)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return atual.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString() ?? string.Empty)
                .Where(x => x.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static string? ObterString(JsonElement item, string nome) =>
            item.TryGetProperty(nome, out var valor) && valor.ValueKind == JsonValueKind.String
                ? valor.GetString()
                : null;

        private static int? ObterInt(JsonElement item, string nome) =>
            item.TryGetProperty(nome, out var valor) && valor.ValueKind == JsonValueKind.Number &&
            valor.TryGetInt32(out var numero)
                ? numero
                : null;

        private static decimal? ObterPrecoPorMilhaoTokens(JsonElement item, string nome)
        {
            if (!TentarObter(item, out var valor, "pricing", nome) ||
                valor.ValueKind != JsonValueKind.String ||
                !decimal.TryParse(valor.GetString(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var precoPorToken))
                return null;

            return precoPorToken * 1_000_000m;
        }

        private static bool TentarObter(JsonElement item, out JsonElement valor,
            params string[] caminho)
        {
            valor = item;
            foreach (var parte in caminho)
            {
                if (valor.ValueKind != JsonValueKind.Object ||
                    !valor.TryGetProperty(parte, out valor))
                    return false;
            }
            return true;
        }
    }
}
