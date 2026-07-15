using ExtratorDocumentos.Application.Dtos.Documentos;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public interface ISugestorSchemaDocumentoService
    {
        Task<ResultadoSugestaoSchemaDto> SugerirAsync(
            string tipoDocumento,
            string? descricaoTipoDocumento,
            string? pais,
            string? idioma,
            ArquivoDocumento arquivo,
            CancellationToken cancellationToken);
    }

    public interface IValidadorSchemaDocumentoService
    {
        ResultadoValidacaoSchemaDto Validar(TipoDocumentoSchemaDto schema);
    }

    public interface IPublicadorSchemaDocumentoService
    {
        Task PublicarAsync(
            Guid tipoDocumentoId,
            Guid schemaId,
            CancellationToken cancellationToken);
    }
}
