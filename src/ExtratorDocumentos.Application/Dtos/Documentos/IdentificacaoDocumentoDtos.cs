namespace ExtratorDocumentos.Application.Dtos.Documentos
{
    public sealed class ResultadoIdentificacaoDocumentoDto
    {
        public Guid DocumentoId { get; set; }
        public string Status { get; set; } = "Pendente";
        public bool ResultadoReutilizado { get; set; }
        public ClassificacaoDocumentoDto Classificacao { get; set; } = new();
        public TitularDocumentoDto Titular { get; set; } = new();
        public IdentificacaoDocumentoDadosDto Identificacao { get; set; } = new();
        public CamposEspecificosDocumentoDto CamposEspecificos { get; set; } = new();
        public List<CampoExtraidoDocumentoDto> CamposExtraidos { get; set; } = new();
        public ValidacaoDocumentoDto Validacao { get; set; } = new();
        public ProcessamentoIdentificacaoDto Processamento { get; set; } = new();
    }

    public sealed class ClassificacaoDocumentoDto
    {
        public string? TipoInformado { get; set; }
        public string? TipoIdentificado { get; set; }
        public string? SubtipoIdentificado { get; set; }
        public bool TipoConfirmado { get; set; }
        public string? PaisEmissor { get; set; }
        public string? NomeDocumentoOriginal { get; set; }
        public List<string> IdiomasIdentificados { get; set; } = new();
        public decimal Confianca { get; set; }
    }

    public sealed class TitularDocumentoDto
    {
        public string? NomeCompleto { get; set; }
        public string? Sobrenome { get; set; }
        public string? Nomes { get; set; }
        public string? DataNascimento { get; set; }
        public string? Sexo { get; set; }
        public string? Nacionalidade { get; set; }
        public string? LocalNascimento { get; set; }
    }

    public sealed class IdentificacaoDocumentoDadosDto
    {
        public string? NumeroDocumento { get; set; }
        public string? NumeroDocumentoNormalizado { get; set; }
        public string? DataEmissao { get; set; }
        public string? DataValidade { get; set; }
        public string? AutoridadeEmissora { get; set; }
        public string? CodigoPaisEmissor { get; set; }
    }

    public sealed class CamposEspecificosDocumentoDto
    {
        public string TipoDocumento { get; set; } = "outro";
        public Dictionary<string, object?> Valores { get; set; } = new();
    }

    public sealed class CampoExtraidoDocumentoDto
    {
        public string Chave { get; set; } = string.Empty;
        public string? RotuloOriginal { get; set; }
        public object? ValorOriginal { get; set; }
        public object? ValorNormalizado { get; set; }
        public string TipoDado { get; set; } = "texto";
        public int? Pagina { get; set; }
        public decimal Confianca { get; set; }
    }

    public sealed class ValidacaoDocumentoDto
    {
        public bool ArquivoLegivel { get; set; }
        public bool DocumentoCompleto { get; set; }
        public bool TipoInformadoConfirmado { get; set; }
        public bool? DocumentoVencido { get; set; }
        public bool DadosMinimosEncontrados { get; set; }
        public bool RequerRevisaoHumana { get; set; }
        public decimal ConfiancaGeral { get; set; }
        public List<string> Alertas { get; set; } = new();
        public List<string> CamposAusentes { get; set; } = new();
        public List<DivergenciaDocumentoDto> Divergencias { get; set; } = new();
    }

    public sealed class DivergenciaDocumentoDto
    {
        public string Campo { get; set; } = string.Empty;
        public string? ValorInformado { get; set; }
        public string? ValorIdentificado { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }

    public sealed class ProcessamentoIdentificacaoDto
    {
        public string HashSha256 { get; set; } = string.Empty;
        public string VersaoSchema { get; set; } = string.Empty;
        public string VersaoExtrator { get; set; } = string.Empty;
        public string? ModeloUtilizado { get; set; }
        public DateTime? ProcessadoEm { get; set; }
    }
}
