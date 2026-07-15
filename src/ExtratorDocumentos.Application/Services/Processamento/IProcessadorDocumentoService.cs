using ExtratorDocumentos.Application.Dtos.Documentos;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public interface IProcessadorDocumentoService
    {
        Task<ResultadoProcessamentoDocumento> ProcessarAsync(
            ArquivoDocumento arquivo,
            CancellationToken cancellationToken);
    }
}
