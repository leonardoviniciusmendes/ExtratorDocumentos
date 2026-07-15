using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Domain;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public interface IProcessadorDocumentoIdentificacaoService
    {
        Task<ResultadoDocumentoPadronizadoDto> ProcessarAsync(
            string tipoDocumento,
            ArquivoDocumento arquivo,
            CancellationToken cancellationToken);
    }
}
