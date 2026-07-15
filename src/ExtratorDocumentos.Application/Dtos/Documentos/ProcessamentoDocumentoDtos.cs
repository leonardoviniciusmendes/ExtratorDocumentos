using ExtratorDocumentos.Domain;

namespace ExtratorDocumentos.Application.Dtos.Documentos
{
    public sealed record ArquivoDocumento(
        string NomeOriginal,
        string MimeType,
        Stream Conteudo);

    public sealed record CaracteristicasArquivo(
        string NomeOriginal,
        string Extensao,
        string MimeType,
        long TamanhoBytes,
        int? QuantidadePaginas,
        bool PossuiTextoExtraivel,
        int QuantidadeCaracteresAproximada,
        bool PossuiImagens,
        bool NecessitaOcr,
        bool PossuiTabelas,
        decimal QualidadeEstimada,
        bool ExigeModeloVisao);

    public sealed record DocumentoIdentificado(
        TipoDocumento Tipo,
        decimal Confianca,
        string ResultadoJson);

    public sealed record ModeloSelecionado(
        string ModeloId,
        string Nome,
        string Objetivo,
        decimal? CustoEntradaPorMilhaoTokens,
        decimal? CustoSaidaPorMilhaoTokens,
        int Prioridade,
        bool RequerVisao);

    public sealed record ResultadoProcessamentoDocumento(
        Guid DocumentoId,
        StatusDocumento Status,
        bool ArquivoReutilizado,
        bool ProcessamentoExecutado,
        TipoDocumento? TipoDocumento,
        string? ModeloIdentificacao,
        string? ModeloExtracao,
        string? Resultado,
        string? Erro);

    public sealed record ResultadoChamadaModelo<T>(
        T? Dados,
        string ModeloId,
        int? TokensEntrada,
        int? TokensSaida,
        decimal? Custo,
        long DuracaoMs,
        bool Sucesso,
        string? Erro,
        string? RequestId);
}
