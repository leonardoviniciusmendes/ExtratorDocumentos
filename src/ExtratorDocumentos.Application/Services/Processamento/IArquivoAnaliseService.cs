using ExtratorDocumentos.Application.Dtos.Documentos;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public interface IArquivoAnaliseService
    {
        Task<CaracteristicasArquivo> AnalisarAsync(
            string nomeOriginal,
            string mimeType,
            byte[] conteudo,
            CancellationToken cancellationToken);
    }
}
