using System.Text.Json.Serialization;

namespace ExtratorDocumentos.Application.Dtos.Documentos
{
    public sealed class ResultadoSugestaoSchemaDto
    {
        public TipoDocumentoSchemaTipoDto TipoDocumento { get; set; } = new();
        public DocumentoReferenciaSchemaDto DocumentoReferencia { get; set; } = new();
        public TipoDocumentoSchemaDto Schema { get; set; } = new();
        public AnaliseSugestaoSchemaDto Analise { get; set; } = new();
        public AnaliseArquivoSugestaoSchemaDto AnaliseArquivo { get; set; } = new();
    }

    public sealed class TipoDocumentoSchemaTipoDto
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
    }

    public sealed class TipoDocumentoSchemaDto
    {
        public Guid Id { get; set; }
        public string Versao { get; set; } = "1.0";
        public string Status { get; set; } = "Rascunho";
        public bool GeradoAutomaticamente { get; set; }
        public List<CampoSchemaSugeridoDto> Campos { get; set; } = new();
        public Dictionary<string, object?> JsonExemplo { get; set; } = new();
    }

    public sealed class DocumentoReferenciaSchemaDto
    {
        public Guid Id { get; set; }
        public string NomeArquivo { get; set; } = string.Empty;
        public string HashSha256 { get; set; } = string.Empty;
    }

    public class CampoSchemaSugeridoDto
    {
        public string Chave { get; set; } = string.Empty;
        public string NomeExibicao { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string TipoDado { get; set; } = "texto";
        public bool ObrigatorioSugerido { get; set; }
        public string OrigemSugestao { get; set; } = "conhecimento_tipo";
        public bool EncontradoNoArquivo { get; set; }
        public decimal? Confianca { get; set; }
        public int Ordem { get; set; }
        public List<string> Aliases { get; set; } = new();
        public string? RegraNormalizacao { get; set; }
        public string? RegraValidacao { get; set; }
        public List<CampoSchemaSugeridoDto> CamposFilhos { get; set; } = new();
        public CampoSchemaSugeridoDto? ItemLista { get; set; }

        [JsonIgnore]
        public bool Obrigatorio
        {
            get => ObrigatorioSugerido;
            set => ObrigatorioSugerido = value;
        }

        [JsonIgnore]
        public List<CampoSchemaSugeridoDto> Campos
        {
            get => CamposFilhos;
            set => CamposFilhos = value;
        }

        [JsonIgnore]
        public CampoSchemaSugeridoDto? Item
        {
            get => ItemLista;
            set => ItemLista = value;
        }
    }

    public sealed class TipoDocumentoCampoDto : CampoSchemaSugeridoDto
    {
    }

    public sealed class TipoDocumentoItemListaDto : CampoSchemaSugeridoDto
    {
        public TipoDocumentoItemListaDto()
        {
            Chave = "item";
            NomeExibicao = "Item";
            TipoDado = "objeto";
        }
    }

    public sealed class AnaliseSugestaoSchemaDto
    {
        public string? ModeloUtilizado { get; set; }
        public decimal Confianca { get; set; }
        public List<string> Alertas { get; set; } = new();
        public int CamposSugeridos { get; set; }
    }

    public sealed class AnaliseArquivoSugestaoSchemaDto
    {
        public List<string> CamposEncontrados { get; set; } = new();
        public List<string> CamposNaoEncontradosMasSugeridos { get; set; } = new();
        public int QuantidadeCamposEncontrados { get; set; }
        public int QuantidadeTotalCamposSugeridos { get; set; }
    }

    public sealed class ResultadoValidacaoSchemaDto
    {
        public bool Valido { get; set; }
        public List<string> Erros { get; set; } = new();
        public List<string> Alertas { get; set; } = new();
    }
}
