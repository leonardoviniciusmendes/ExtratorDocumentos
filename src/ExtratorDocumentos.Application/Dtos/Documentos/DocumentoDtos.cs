using ExtratorDocumentos.Domain;

namespace ExtratorDocumentos.Application.Dtos.Documentos
{
    public sealed class DocumentoQuery
    {
        public string? Cpf { get; set; }
        public TipoParentesco? TipoParentesco { get; set; }
        public TipoDocumento? Tipo { get; set; }
        public StatusDocumento? Status { get; set; }
        public bool IncluirExcluidos { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
    public sealed record DocumentoResponse(Guid Id, string? Cpf, string? CpfDependente,
        string? Cnpj, PapelDocumento Papel, TipoParentesco TipoParentesco,
        TipoDocumento Tipo, StatusDocumento Status, string? Observacoes, int VersaoAtual, bool Excluido,
        DateTime CriadoEm, DateTime AtualizadoEm, StatusExtracao StatusExtracao,
        string? ErroExtracao, DateTime? ExtraidoEm);
    public sealed record DocumentoVersaoResponse(Guid Id, int Versao, string NomeArquivo,
        string TipoConteudo, long TamanhoBytes, string HashSha256, DateTime CriadoEm);
    public sealed record DocumentoHistoricoResponse(Guid Id, string Acao,
        StatusDocumento? StatusAnterior, StatusDocumento? StatusNovo, string? Observacao,
        string? Usuario, DateTime Data);
    public sealed record IdentificacaoExtraidaResponse(Guid Id, Guid DocumentoId,
        Guid DocumentoVersaoId, string? NomeCompleto, string? Cpf, string? Rg,
        string? OrgaoEmissor, string? UfEmissao, DateTime? DataNascimento,
        string? Naturalidade, string? Nacionalidade, string? NomeMae, string? NomePai,
        string? NumeroCnh, string? CategoriaCnh, DateTime? ValidadeCnh,
        DateTime? DataPrimeiraHabilitacao, DateTime? DataEmissao, string? LocalEmissao,
        string? NumeroRenach, string? ObservacoesCnh, string? Cnpj,
        string? RazaoSocial, string? NomeFantasia,
        string? EmissorDocumento, string? NumeroCliente, string? NumeroInstalacao,
        string? MesReferencia, DateTime? DataVencimento,
        string? MatriculaCertidao, string? Livro, string? Folha, string? Termo,
        decimal Confianca, string Provedor, DateTime CriadoEm);
    public sealed record EnderecoExtraidoResponse(Guid Id, Guid DocumentoId,
        Guid DocumentoVersaoId, string? Cpf, string? Cnpj, string? Cep,
        string? Logradouro, string? Numero, string? Complemento, string? Bairro,
        string? Cidade, string? Uf, string? FonteDocumento, DateTime CriadoEm);
    public sealed record VinculoExtracaoResponse(string? Cpf, string? Cnpj,
        Guid DocumentoId, Guid DocumentoVersaoId);
    public sealed record DadosExtraidosResponse(
        IdentificacaoExtraidaResponse? Identificacao,
        IReadOnlyList<EnderecoExtraidoResponse> Enderecos,
        VinculoExtracaoResponse? Vinculo);
    public sealed class AtualizarDocumentoRequest
    {
        public StatusDocumento? Status { get; set; }
        public string? Observacoes { get; set; }
        public string? Usuario { get; set; }
    }
}
