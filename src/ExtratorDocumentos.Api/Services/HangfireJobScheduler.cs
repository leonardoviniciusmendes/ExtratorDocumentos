using ExtratorDocumentos.Application.Jobs;
using Hangfire;

namespace ExtratorDocumentos.Api.Services;

public sealed class HangfireJobScheduler
{
    private readonly IRecurringJobManager _manager;
    private readonly IConfiguration _configuration;
    private readonly AtualizarOpenRouterModelosJob _openRouterModelosJob;
    private readonly ILogger<HangfireJobScheduler> _logger;

    public HangfireJobScheduler(IRecurringJobManager manager, IConfiguration configuration,
        AtualizarOpenRouterModelosJob openRouterModelosJob,
        ILogger<HangfireJobScheduler> logger)
    {
        _manager = manager;
        _configuration = configuration;
        _openRouterModelosJob = openRouterModelosJob;
        _logger = logger;
    }

    public void RegistrarJobs()
    {
        var atualizarModelos = _configuration.GetValue<bool?>("OpenRouter:AtualizacaoModelos:Habilitada") ??
            _configuration.GetValue<bool?>("Jobs:OpenRouterModelos:Habilitado") ?? true;
        if (atualizarModelos)
        {
            var cron = _configuration["OpenRouter:AtualizacaoModelos:Cron"] ??
                _configuration["Jobs:OpenRouterModelos:Cron"];
            if (string.IsNullOrWhiteSpace(cron))
                cron = Cron.Daily();

            _manager.AddOrUpdate<HangfireJobScheduler>("atualizar-openrouter-modelos",
                x => x.ExecutarAtualizacaoOpenRouterModelosAsync(), cron);
            _logger.LogInformation(
                "Job recorrente de modelos OpenRouter registrado. Cron: {Cron}", cron);
        }
    }

    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task ExecutarAtualizacaoOpenRouterModelosAsync()
    {
        await _openRouterModelosJob.ExecutarAsync(CancellationToken.None);
    }
}
