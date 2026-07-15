using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public sealed class OpenRouterModelSelector : IOpenRouterModelCandidateSelector
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;

        public OpenRouterModelSelector(AppDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public Task<ModeloSelecionado> SelecionarParaIdentificacaoAsync(
            CaracteristicasArquivo arquivo, CancellationToken cancellationToken) =>
            SelecionarCandidatosParaIdentificacaoAsync(arquivo, cancellationToken)
                .ContinueWith(x => x.Result.First(), cancellationToken);

        public Task<ModeloSelecionado> SelecionarParaExtracaoAsync(
            DocumentoIdentificado documento, CaracteristicasArquivo arquivo,
            CancellationToken cancellationToken) =>
            SelecionarCandidatosParaExtracaoAsync(documento, arquivo, cancellationToken)
                .ContinueWith(x => x.Result.First(), cancellationToken);

        public Task<IReadOnlyList<ModeloSelecionado>> SelecionarCandidatosParaIdentificacaoAsync(
            CaracteristicasArquivo arquivo, CancellationToken cancellationToken) =>
            SelecionarAsync("identificacao", null, arquivo, cancellationToken);

        public Task<IReadOnlyList<ModeloSelecionado>> SelecionarCandidatosParaExtracaoAsync(
            DocumentoIdentificado documento, CaracteristicasArquivo arquivo,
            CancellationToken cancellationToken) =>
            SelecionarAsync("extracao", documento.Tipo, arquivo, cancellationToken);

        private async Task<IReadOnlyList<ModeloSelecionado>> SelecionarAsync(string objetivo,
            TipoDocumento? tipo, CaracteristicasArquivo arquivo,
            CancellationToken cancellationToken)
        {
            var custoMaximo = decimal.TryParse(
                _configuration["OpenRouter:Selecao:CustoMaximoPorChamadaUsd"],
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var custoConfigurado)
                    ? custoConfigurado
                    : (decimal?)null;
            var contextoMinimo = Math.Max(4096,
                arquivo.QuantidadeCaracteresAproximada / 3);
            var requerVisao = arquivo.ExigeModeloVisao;
            var query = _db.OpenRouterModelos.AsNoTracking()
                .Where(x => x.Ativo && x.Disponivel && x.Permitido && !x.Bloqueado)
                .Where(x => x.SuportaJson || x.SuportaStructuredOutputs)
                .Where(x => !requerVisao || x.AceitaImagem || x.AceitaArquivo)
                .Where(x => x.ContextoTokens == null || x.ContextoTokens >= contextoMinimo);

            if (custoMaximo.HasValue)
            {
                var maxPorMilhao = custoMaximo.Value * 1_000_000m /
                    Math.Max(contextoMinimo, 1);
                query = query.Where(x => x.PrecoEntradaPorMilhaoTokens == null ||
                    x.PrecoEntradaPorMilhaoTokens <= maxPorMilhao);
            }

            var modelos = await query.ToListAsync(cancellationToken);
            if (modelos.Count == 0)
                throw new InvalidOperationException(
                    "Nenhum modelo OpenRouter permitido atende aos requisitos do arquivo.");

            return modelos
                .OrderBy(x => objetivo == "identificacao"
                    ? x.PrioridadeIdentificacao : x.PrioridadeExtracao)
                .ThenBy(x => PenalidadeComplexidade(x, objetivo, tipo, arquivo))
                .ThenBy(x => x.PrecoEntradaPorMilhaoTokens ?? decimal.MaxValue)
                .ThenBy(x => x.PrecoSaidaPorMilhaoTokens ?? decimal.MaxValue)
                .ThenBy(x => x.Id)
                .Select(x => ToSelecionado(x, objetivo, requerVisao))
                .ToList();
        }

        private static int PenalidadeComplexidade(OpenRouterModelo modelo,
            string objetivo, TipoDocumento? tipo, CaracteristicasArquivo arquivo)
        {
            var penalidade = 0;
            if (objetivo == "extracao" &&
                (arquivo.PossuiTabelas || arquivo.QuantidadePaginas > 3))
                penalidade -= modelo.SuportaStructuredOutputs ? 5 : 0;
            if (tipo is TipoDocumento.ContratoSocial or TipoDocumento.ExtratoBancario)
                penalidade -= modelo.ContextoTokens >= 32000 ? 4 : 0;
            if (!arquivo.ExigeModeloVisao && (modelo.AceitaImagem || modelo.AceitaArquivo))
                penalidade += 1;
            return penalidade;
        }

        private static ModeloSelecionado ToSelecionado(OpenRouterModelo modelo,
            string objetivo, bool requerVisao) =>
            new(modelo.Id, modelo.Nome, objetivo,
                modelo.PrecoEntradaPorMilhaoTokens,
                modelo.PrecoSaidaPorMilhaoTokens,
                objetivo == "identificacao"
                    ? modelo.PrioridadeIdentificacao
                    : modelo.PrioridadeExtracao,
                requerVisao);
    }
}
