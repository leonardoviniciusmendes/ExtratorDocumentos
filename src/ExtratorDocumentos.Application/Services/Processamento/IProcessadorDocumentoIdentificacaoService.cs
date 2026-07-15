using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Domain;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public interface IProcessadorDocumentoIdentificacaoService
    {
        Task<ResultadoIdentificacaoDocumentoDto> ProcessarAsync(
            TipoDocumentoProcessamento tipoDocumento,
            ArquivoDocumento arquivo,
            CancellationToken cancellationToken);
    }
}
