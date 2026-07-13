namespace ExtratorDocumentos.Domain
{
    public enum TipoDocumento
    {
        RG = 0, CPF = 1, CNH = 2, ComprovanteResidencia = 3, ContaLuz = 4,
        CertidaoNascimento = 5, CertidaoCasamento = 6, ContratoSocial = 7,
        CartaoCNPJ = 8, ContaAgua = 9, ContaTelefone = 10, ContaInternet = 11,
        ContaGas = 12, FaturaCartaoCredito = 13, ExtratoBancario = 14,
        ContratoLocacao = 15, IPTU = 16, Elegibilidade = 17,
        FichaAssociativa = 18, DocumentoOficialComSelfie = 19, Outros = 99
    }

    public enum StatusDocumento
    {
        Pendente = 0, Disponivel = 1, EmAnalise = 2, Aprovado = 3,
        Rejeitado = 4, Excluido = 5, Erro = 6
    }

    public enum StatusExtracao
    {
        Pendente = 0, Processando = 1, Processado = 2, RevisaoManual = 3, Erro = 4
    }

    public enum TipoParentesco
    {
        Titular = 0, Conjuge = 1, Filho = 2, Pai = 3, Mae = 4,
        Socio = 5, RepresentanteLegal = 6, Outros = 99
    }

    public enum PapelDocumento
    {
        Titular = 0,
        Dependente = 1,
        Empresa = 2
    }

    public class Documento
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Cpf { get; set; }
        public string? CpfDependente { get; set; }
        public string? Cnpj { get; set; }
        public PapelDocumento Papel { get; set; } = PapelDocumento.Titular;
        public TipoParentesco TipoParentesco { get; set; } = TipoParentesco.Titular;
        public TipoDocumento Tipo { get; set; }
        public StatusDocumento Status { get; set; } = StatusDocumento.Pendente;
        public string? Observacoes { get; set; }
        public int VersaoAtual { get; set; } = 1;
        public bool Excluido { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
        public DateTime? ExcluidoEm { get; set; }
        public StatusExtracao StatusExtracao { get; set; } = StatusExtracao.Pendente;
        public string? ErroExtracao { get; set; }
        public DateTime? ExtraidoEm { get; set; }
        public IdentificacaoExtraida? Identificacao { get; set; }
        public ICollection<EnderecoExtraido> Enderecos { get; set; } = new List<EnderecoExtraido>();
        public ICollection<DocumentoVersao> Versoes { get; set; } = new List<DocumentoVersao>();
        public ICollection<DocumentoHistorico> Historico { get; set; } = new List<DocumentoHistorico>();
    }

    public class DocumentoVersao
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentoId { get; set; }
        public Documento? Documento { get; set; }
        public int Versao { get; set; }
        public string NomeArquivo { get; set; } = string.Empty;
        public string TipoConteudo { get; set; } = "application/octet-stream";
        public long TamanhoBytes { get; set; }
        public string HashSha256 { get; set; } = string.Empty;
        public string ChaveStorage { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }

    public class DocumentoHistorico
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentoId { get; set; }
        public Documento? Documento { get; set; }
        public string Acao { get; set; } = string.Empty;
        public StatusDocumento? StatusAnterior { get; set; }
        public StatusDocumento? StatusNovo { get; set; }
        public string? Observacao { get; set; }
        public string? Usuario { get; set; }
        public DateTime Data { get; set; } = DateTime.UtcNow;
    }

    public class IdentificacaoExtraida
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentoId { get; set; }
        public Documento? Documento { get; set; }
        public Guid DocumentoVersaoId { get; set; }
        public DocumentoVersao? DocumentoVersao { get; set; }
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
        public string Provedor { get; set; } = string.Empty;
        public string DadosBrutosJson { get; set; } = "{}";
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }

    public class EnderecoExtraido
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentoId { get; set; }
        public Documento? Documento { get; set; }
        public Guid DocumentoVersaoId { get; set; }
        public DocumentoVersao? DocumentoVersao { get; set; }
        public string? Cpf { get; set; }
        public string? Cnpj { get; set; }
        public string? Cep { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Uf { get; set; }
        public string? FonteDocumento { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
