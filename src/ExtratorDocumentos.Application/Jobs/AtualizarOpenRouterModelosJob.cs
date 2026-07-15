using ExtratorDocumentos.Application.Services.Extracao;
using ExtratorDocumentos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtratorDocumentos.Application.Jobs
{
    public sealed class AtualizarOpenRouterModelosJob
    {
        private const string NomeLock = "extrator-documentos-openrouter-modelos";
        private readonly AppDbContext _db;
        private readonly OpenRouterModelosService _modelosService;
        private readonly ILogger<AtualizarOpenRouterModelosJob> _logger;

        public AtualizarOpenRouterModelosJob(AppDbContext db,
            OpenRouterModelosService modelosService,
            ILogger<AtualizarOpenRouterModelosJob> logger)
        {
            _db = db;
            _modelosService = modelosService;
            _logger = logger;
        }

        public async Task<AtualizarOpenRouterModelosResult> ExecutarAsync(
            CancellationToken cancellationToken)
        {
            await _db.Database.OpenConnectionAsync(cancellationToken);
            var lockAdquirido = false;

            try
            {
                lockAdquirido = await TentarAdquirirLockAsync(cancellationToken);
                if (!lockAdquirido)
                {
                    _logger.LogInformation(
                        "Atualizacao de modelos OpenRouter ignorada porque outra instancia esta executando o job.");
                    return new AtualizarOpenRouterModelosResult(0, 0, 0, 0);
                }

                var resultado = await _modelosService.SincronizarAsync(cancellationToken);
                _logger.LogInformation(
                    "Modelos OpenRouter atualizados. Remotos: {Remotos}; Novos: {Novos}; Atualizados: {Atualizados}; Indisponiveis: {Indisponiveis}",
                    resultado.Remotos,
                    resultado.Novos,
                    resultado.Atualizados,
                    resultado.Indisponiveis);

                return resultado;
            }
            finally
            {
                if (lockAdquirido)
                    await LiberarLockAsync();

                await _db.Database.CloseConnectionAsync();
            }
        }

        private async Task<bool> TentarAdquirirLockAsync(CancellationToken cancellationToken)
        {
            await using var command = _db.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT GET_LOCK('{NomeLock}', 0);";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) == 1;
        }

        private async Task LiberarLockAsync()
        {
            await using var command = _db.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT RELEASE_LOCK('{NomeLock}');";
            await command.ExecuteScalarAsync(CancellationToken.None);
        }
    }

    public sealed record AtualizarOpenRouterModelosResult(
        int Remotos,
        int Novos,
        int Atualizados,
        int Indisponiveis);
}
