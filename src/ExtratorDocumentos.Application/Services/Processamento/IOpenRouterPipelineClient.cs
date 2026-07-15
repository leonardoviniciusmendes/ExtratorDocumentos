using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Application.Services.Extracao;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public interface IOpenRouterPipelineClient
    {
        Task<ResultadoChamadaModelo<DocumentoIdentificado>> IdentificarAsync(
            ModeloSelecionado modelo,
            CaracteristicasArquivo caracteristicas,
            byte[] conteudo,
            CancellationToken cancellationToken);

        Task<ResultadoChamadaModelo<ExtracaoDocumentoResult>> ExtrairAsync(
            ModeloSelecionado modelo,
            DocumentoIdentificado documento,
            CaracteristicasArquivo caracteristicas,
            byte[] conteudo,
            CancellationToken cancellationToken);

        Task<ResultadoChamadaModelo<ResultadoIdentificacaoDocumentoDto>> ExtrairIdentificacaoUniversalAsync(
            ModeloSelecionado modelo,
            string tipoDocumentoSolicitado,
            CaracteristicasArquivo caracteristicas,
            byte[] conteudo,
            string? schemaJson,
            CancellationToken cancellationToken);
    }
}
