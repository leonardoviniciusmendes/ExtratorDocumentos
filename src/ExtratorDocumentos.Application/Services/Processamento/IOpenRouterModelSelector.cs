using ExtratorDocumentos.Application.Dtos.Documentos;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public interface IOpenRouterModelSelector
    {
        Task<ModeloSelecionado> SelecionarParaIdentificacaoAsync(
            CaracteristicasArquivo arquivo,
            CancellationToken cancellationToken);

        Task<ModeloSelecionado> SelecionarParaExtracaoAsync(
            DocumentoIdentificado documento,
            CaracteristicasArquivo arquivo,
            CancellationToken cancellationToken);
    }

    public interface IOpenRouterModelCandidateSelector : IOpenRouterModelSelector
    {
        Task<IReadOnlyList<ModeloSelecionado>> SelecionarCandidatosParaIdentificacaoAsync(
            CaracteristicasArquivo arquivo,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<ModeloSelecionado>> SelecionarCandidatosParaExtracaoAsync(
            DocumentoIdentificado documento,
            CaracteristicasArquivo arquivo,
            CancellationToken cancellationToken);
    }
}
