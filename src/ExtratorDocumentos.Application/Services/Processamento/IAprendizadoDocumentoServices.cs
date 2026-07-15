using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Domain;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public interface IAssinaturaEstruturalDocumentoService
    {
        AssinaturaEstruturalDocumento Gerar(
            ResultadoIdentificacaoDocumentoDto resultado,
            CaracteristicasArquivo caracteristicas);
    }

    public interface IIdentificadorTipoDocumentoService
    {
        Task<ResultadoIdentificacaoTipoDocumento> IdentificarAsync(
            AssinaturaEstruturalDocumento assinatura,
            CancellationToken cancellationToken);
    }

    public interface IGeradorSchemaDocumentoService
    {
        Task<ResultadoGeracaoSchemaDocumento> GerarAsync(
            ResultadoIdentificacaoDocumentoDto resultado,
            CancellationToken cancellationToken);
    }

    public interface IAplicadorSchemaDocumentoService
    {
        Task<ResultadoDocumentoPadronizadoDto> AplicarAsync(
            TipoDocumentoSchema schema,
            ResultadoIdentificacaoDocumentoDto extracao,
            CancellationToken cancellationToken);
    }

    public interface ITipoDocumentoAprendizadoService
    {
        Task<(TipoDocumento Tipo, TipoDocumentoSchema Schema, bool Reutilizado,
            decimal Similaridade, bool RequerRevisao)> ObterOuCriarAsync(
            ResultadoIdentificacaoTipoDocumento identificacao,
            AssinaturaEstruturalDocumento assinatura,
            ResultadoIdentificacaoDocumentoDto extracao,
            CancellationToken cancellationToken);

        Task RegistrarCamposAdicionaisAsync(
            TipoDocumento tipoDocumento,
            IReadOnlyDictionary<string, object?> camposAdicionais,
            CancellationToken cancellationToken);

        Task<TipoDocumentoSchema> AtivarSchemaAsync(
            Guid tipoDocumentoId,
            Guid schemaId,
            CancellationToken cancellationToken);

        Task<TipoDocumentoSchema> AprovarCampoSugeridoAsync(
            Guid tipoDocumentoId,
            Guid campoId,
            CancellationToken cancellationToken);
    }
}
