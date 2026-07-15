using ExtratorDocumentos.Domain;

namespace ExtratorDocumentos.Application.Dtos.Documentos
{
    public sealed class DocumentoQuery
    {
        public string? Cpf { get; set; }
        public TipoParentesco? TipoParentesco { get; set; }
        public TipoDocumentoLegado? Tipo { get; set; }
        public StatusDocumento? Status { get; set; }
        public bool IncluirExcluidos { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
    public sealed record DocumentoResponse(Guid Id, string? Cpf, string? CpfDependente,
        string? Cnpj, PapelDocumento Papel, TipoParentesco TipoParentesco,
        TipoDocumentoLegado Tipo, StatusDocumento Status, string? Observacoes, int VersaoAtual, bool Excluido,
        DateTime CriadoEm, DateTime AtualizadoEm, StatusExtracao StatusExtracao,
        string? ErroExtracao, DateTime? ExtraidoEm);
    public sealed record DocumentoVersaoResponse(Guid Id, int Versao, string NomeArquivo,
        string TipoConteudo, long TamanhoBytes, string HashSha256, DateTime CriadoEm);
    public sealed record DocumentoHistoricoResponse(Guid Id, string Acao,
        StatusDocumento? StatusAnterior, StatusDocumento? StatusNovo, string? Observacao,
        string? Usuario, DateTime Data);
    public sealed class AtualizarDocumentoRequest
    {
        public StatusDocumento? Status { get; set; }
        public string? Observacoes { get; set; }
        public string? Usuario { get; set; }
    }
}
