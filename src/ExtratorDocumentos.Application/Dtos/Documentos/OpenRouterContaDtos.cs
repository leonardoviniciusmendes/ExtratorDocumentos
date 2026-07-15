namespace ExtratorDocumentos.Application.Dtos.Documentos
{
    public sealed record OpenRouterCreditosResponse(
        decimal? CreditosTotais,
        decimal? UsoTotal,
        decimal? SaldoDisponivel);

    public sealed record OpenRouterUsoResponse(
        string? Provedor,
        string? Modelo,
        string? RequisicaoId,
        int? TokensEntrada,
        int? TokensSaida,
        int? TokensTotais,
        decimal? CustoUsd);
}
