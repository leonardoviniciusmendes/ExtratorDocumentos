namespace ExtratorDocumentos.Application.Dtos.Documentos
{
    public sealed record OpenRouterModeloResponse(
        string Id,
        string Nome,
        string Objetivo,
        int? ContextoTokens,
        bool AceitaArquivo,
        bool AceitaImagem,
        bool SaidaTexto,
        bool SuportaJson,
        bool SuportaStructuredOutputs,
        decimal? PrecoEntradaPorMilhaoTokens,
        decimal? PrecoSaidaPorMilhaoTokens);
}
