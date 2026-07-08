using ExtratorDocumentos.Domain;

namespace ExtratorDocumentos.Application.Services.Extracao
{
    public interface IDocumentoExtracaoProvider
    {
        string Nome { get; }
        Task<ExtracaoDocumentoResult?> ExtrairAsync(TipoDocumento tipo, string nomeArquivo,
            string tipoConteudo, Stream conteudo, CancellationToken cancellationToken);
    }

    public sealed class ExtracaoDocumentoResult
    {
        public string? NomeCompleto { get; set; }
        public string? Cpf { get; set; }
        public string? Rg { get; set; }
        public string? OrgaoEmissor { get; set; }
        public string? UfEmissao { get; set; }
        public DateTime? DataNascimento { get; set; }
        public string? Naturalidade { get; set; }
        public string? Nacionalidade { get; set; }
        public string? NomeMae { get; set; }
        public string? NomePai { get; set; }
        public string? NumeroCnh { get; set; }
        public string? CategoriaCnh { get; set; }
        public DateTime? ValidadeCnh { get; set; }
        public DateTime? DataPrimeiraHabilitacao { get; set; }
        public DateTime? DataEmissao { get; set; }
        public string? LocalEmissao { get; set; }
        public string? NumeroRenach { get; set; }
        public string? ObservacoesCnh { get; set; }
        public string? Cnpj { get; set; }
        public string? RazaoSocial { get; set; }
        public string? NomeFantasia { get; set; }
        public string? Logradouro { get; set; }
        public string? NumeroEndereco { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public string? Cep { get; set; }
        public string? EmissorDocumento { get; set; }
        public string? NumeroCliente { get; set; }
        public string? NumeroInstalacao { get; set; }
        public string? MesReferencia { get; set; }
        public DateTime? DataVencimento { get; set; }
        public string? MatriculaCertidao { get; set; }
        public string? Livro { get; set; }
        public string? Folha { get; set; }
        public string? Termo { get; set; }
        public decimal Confianca { get; set; }
        public string DadosBrutosJson { get; set; } = "{}";
    }
}
