namespace ExtratorDocumentos.Application.Dtos.Documentos
{
    public sealed class AssinaturaEstruturalDocumento
    {
        public List<string> RotulosEncontrados { get; set; } = new();
        public List<string> SecoesEncontradas { get; set; } = new();
        public string? Idioma { get; set; }
        public string? PaisEmissor { get; set; }
        public string? CategoriaSugerida { get; set; }
        public bool PossuiTabela { get; set; }
        public bool PossuiMrz { get; set; }
        public bool PossuiFoto { get; set; }
        public bool PossuiAssinatura { get; set; }
        public int? QuantidadePaginas { get; set; }
    }

    public sealed class ResultadoIdentificacaoTipoDocumento
    {
        public Guid? TipoDocumentoId { get; set; }
        public Guid? TipoDocumentoSchemaId { get; set; }
        public string CodigoTipo { get; set; } = string.Empty;
        public decimal Similaridade { get; set; }
        public bool TipoExistente { get; set; }
        public bool RequerRevisao { get; set; }
    }

    public sealed class ResultadoGeracaoSchemaDocumento
    {
        public string CodigoTipo { get; set; } = string.Empty;
        public string NomeTipo { get; set; } = string.Empty;
        public string Versao { get; set; } = "1.0";
        public string SchemaJson { get; set; } = "{}";
        public List<CampoSchemaDocumentoDto> Campos { get; set; } = new();
    }

    public sealed class CampoSchemaDocumentoDto
    {
        public string Chave { get; set; } = string.Empty;
        public string NomeExibicao { get; set; } = string.Empty;
        public string TipoDado { get; set; } = "texto";
        public bool Obrigatorio { get; set; }
        public List<string> Aliases { get; set; } = new();
        public string? RegraNormalizacao { get; set; }
        public string? RegraValidacao { get; set; }
        public int Ordem { get; set; }
    }

    public sealed class ResultadoDocumentoPadronizadoDto
    {
        public Guid DocumentoId { get; set; }
        public Guid DocumentoExtracaoId { get; set; }
        public bool ResultadoReutilizado { get; set; }
        public TipoDocumentoResultadoDto TipoDocumento { get; set; } = new();
        public Dictionary<string, object?> Dados { get; set; } = new();
        public Dictionary<string, object?> CamposAdicionais { get; set; } = new();
        public ValidacaoSchemaDocumentoDto Validacao { get; set; } = new();
        public ProcessamentoDocumentoPadronizadoDto Processamento { get; set; } = new();
    }

    public sealed class TipoDocumentoResultadoDto
    {
        public Guid? Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public Guid? SchemaId { get; set; }
        public string SchemaVersao { get; set; } = string.Empty;
        public decimal Similaridade { get; set; }
        public bool TipoReutilizado { get; set; }
        public bool TipoConfirmado { get; set; }
    }

    public sealed class ValidacaoSchemaDocumentoDto
    {
        public bool SchemaAplicado { get; set; }
        public bool DadosObrigatoriosEncontrados { get; set; }
        public bool RequerRevisaoHumana { get; set; }
        public List<string> Alertas { get; set; } = new();
    }

    public sealed class ProcessamentoDocumentoPadronizadoDto
    {
        public string? ModeloUtilizado { get; set; }
        public string VersaoExtrator { get; set; } = string.Empty;
        public DateTime? ProcessadoEm { get; set; }
    }
}
