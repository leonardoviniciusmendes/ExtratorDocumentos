namespace ExtratorDocumentos.Domain
{
    public enum TipoDocumentoLegado
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
        Rejeitado = 4, Excluido = 5, Erro = 6, Concluido = 7, Processando = 8
    }

    public enum StatusExtracao
    {
        Pendente = 0, Processando = 1, Processado = 2, RevisaoManual = 3, Erro = 4,
        Concluido = 5, ConcluidoComAlertas = 6, RequerRevisao = 7
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
        public string NomeOriginal { get; set; } = string.Empty;
        public string HashSha256 { get; set; } = string.Empty;
        public string Extensao { get; set; } = string.Empty;
        public string MimeType { get; set; } = "application/octet-stream";
        public long TamanhoBytes { get; set; }
        public PapelDocumento Papel { get; set; } = PapelDocumento.Titular;
        public TipoParentesco TipoParentesco { get; set; } = TipoParentesco.Titular;
        public TipoDocumentoLegado Tipo { get; set; }
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
        public TipoDocumentoLegado? TipoDocumentoIdentificado { get; set; }
        public decimal? ConfiancaIdentificacao { get; set; }
        public string? ResultadoIdentificacaoJson { get; set; }
        public string? ResultadoExtracaoJson { get; set; }
        public string? ModeloIdentificacao { get; set; }
        public string? ModeloExtracao { get; set; }
        public string VersaoExtrator { get; set; } = "1";
        public string VersaoSchema { get; set; } = "1";
        public DateTime? ProcessadoEm { get; set; }
        public string? ErroProcessamento { get; set; }
        public ICollection<DocumentoVersao> Versoes { get; set; } = new List<DocumentoVersao>();
        public ICollection<DocumentoHistorico> Historico { get; set; } = new List<DocumentoHistorico>();
        public ICollection<UsoOpenRouter> UsosOpenRouter { get; set; } = new List<UsoOpenRouter>();
        public ICollection<DocumentoExtracao> Extracoes { get; set; } = new List<DocumentoExtracao>();
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

    public class OpenRouterModelo
    {
        public string Id { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Objetivo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int? ContextoTokens { get; set; }
        public bool AceitaArquivo { get; set; }
        public bool AceitaImagem { get; set; }
        public bool SaidaTexto { get; set; }
        public bool SuportaJson { get; set; }
        public bool SuportaStructuredOutputs { get; set; }
        public decimal? PrecoEntradaPorMilhaoTokens { get; set; }
        public decimal? PrecoSaidaPorMilhaoTokens { get; set; }
        public bool Disponivel { get; set; } = true;
        public bool Ativo { get; set; } = true;
        public bool Permitido { get; set; } = true;
        public bool Bloqueado { get; set; }
        public int PrioridadeIdentificacao { get; set; } = 100;
        public int PrioridadeExtracao { get; set; } = 100;
        public DateTime PrimeiroVistoEm { get; set; } = DateTime.UtcNow;
        public DateTime UltimoVistoEm { get; set; } = DateTime.UtcNow;
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    }

    public class UsoOpenRouter
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentoId { get; set; }
        public Documento? Documento { get; set; }
        public Guid? DocumentoExtracaoId { get; set; }
        public DocumentoExtracao? DocumentoExtracao { get; set; }
        public string ModeloId { get; set; } = string.Empty;
        public string Objetivo { get; set; } = string.Empty;
        public int? TokensEntrada { get; set; }
        public int? TokensSaida { get; set; }
        public decimal? Custo { get; set; }
        public long DuracaoMs { get; set; }
        public bool Sucesso { get; set; }
        public string? Erro { get; set; }
        public string? RequestId { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }

    public class DocumentoExtracao
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentoId { get; set; }
        public Documento? Documento { get; set; }
        public Guid? TipoDocumentoId { get; set; }
        public Guid? TipoDocumentoSchemaId { get; set; }
        public string TipoDocumentoSolicitado { get; set; } = string.Empty;
        public string? TipoDocumentoIdentificado { get; set; }
        public string VersaoSchema { get; set; } = string.Empty;
        public string VersaoExtrator { get; set; } = string.Empty;
        public StatusExtracao Status { get; set; } = StatusExtracao.Pendente;
        public decimal? Confianca { get; set; }
        public decimal? SimilaridadeTipo { get; set; }
        public bool TipoReutilizado { get; set; }
        public string? ResultadoJson { get; set; }
        public string? ModeloIdentificacao { get; set; }
        public string? ModeloExtracao { get; set; }
        public string? ErroCodigo { get; set; }
        public string? ErroMensagem { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessadoEm { get; set; }
        public TipoDocumento? TipoDocumento { get; set; }
        public TipoDocumentoSchema? TipoDocumentoSchema { get; set; }
        public ICollection<UsoOpenRouter> UsosOpenRouter { get; set; } = new List<UsoOpenRouter>();
    }

    public class TipoDocumento
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public bool Ativo { get; set; } = true;
        public bool Confirmado { get; set; }
        public string? AssinaturaEstruturalJson { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
        public ICollection<TipoDocumentoSchema> Schemas { get; set; } = new List<TipoDocumentoSchema>();
        public ICollection<TipoDocumentoCampoSugerido> CamposSugeridos { get; set; } = new List<TipoDocumentoCampoSugerido>();
        public ICollection<DocumentoExtracao> Extracoes { get; set; } = new List<DocumentoExtracao>();
    }

    public class TipoDocumentoSchema
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TipoDocumentoId { get; set; }
        public string Versao { get; set; } = "1.0";
        public string SchemaJson { get; set; } = "{}";
        public bool Ativo { get; set; } = true;
        public bool GeradoAutomaticamente { get; set; } = true;
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        public TipoDocumento TipoDocumento { get; set; } = null!;
        public ICollection<TipoDocumentoCampo> Campos { get; set; } = new List<TipoDocumentoCampo>();
        public ICollection<DocumentoExtracao> Extracoes { get; set; } = new List<DocumentoExtracao>();
    }

    public class TipoDocumentoCampo
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TipoDocumentoSchemaId { get; set; }
        public string Chave { get; set; } = string.Empty;
        public string NomeExibicao { get; set; } = string.Empty;
        public string TipoDado { get; set; } = "texto";
        public bool Obrigatorio { get; set; }
        public string? AliasesJson { get; set; }
        public string? RegraNormalizacao { get; set; }
        public string? RegraValidacao { get; set; }
        public int Ordem { get; set; }
        public TipoDocumentoSchema Schema { get; set; } = null!;
    }

    public class TipoDocumentoCampoSugerido
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TipoDocumentoId { get; set; }
        public string Chave { get; set; } = string.Empty;
        public string TipoDado { get; set; } = "texto";
        public int QuantidadeOcorrencias { get; set; }
        public bool Aprovado { get; set; }
        public DateTime PrimeiraOcorrenciaEm { get; set; } = DateTime.UtcNow;
        public DateTime UltimaOcorrenciaEm { get; set; } = DateTime.UtcNow;
        public TipoDocumento TipoDocumento { get; set; } = null!;
    }
}
