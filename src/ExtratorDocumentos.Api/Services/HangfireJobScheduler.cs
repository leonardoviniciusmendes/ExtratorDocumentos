using ExtratorDocumentos.Application.Jobs;
using Hangfire;

namespace ExtratorDocumentos.Api.Services;

public sealed class HangfireJobScheduler
{
    private readonly IRecurringJobManager _manager;
    private readonly IConfiguration _configuration;
    private readonly ProcessarDocumentosPendentesJob _job;
    private readonly ILogger<HangfireJobScheduler> _logger;

    public HangfireJobScheduler(IRecurringJobManager manager, IConfiguration configuration,
        ProcessarDocumentosPendentesJob job,
        ILogger<HangfireJobScheduler> logger)
    {
        _manager = manager; _configuration = configuration; _job = job; _logger = logger;
    }

    public void RegistrarJobs()
    {
        var intervalo = _configuration.GetValue<int?>("Jobs:ProcessarDocumentos:IntervaloMinutos") ?? 5;
        intervalo = intervalo <= 0 ? 5 : intervalo;
        _manager.AddOrUpdate<HangfireJobScheduler>("processar-documentos-pendentes",
            x => x.ExecutarProcessamentoAsync(), Cron.MinuteInterval(intervalo));
        _logger.LogInformation("Job recorrente de documentos registrado. Intervalo: {Intervalo} minutos", intervalo);
    }

    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task ExecutarProcessamentoAsync()
    {
        await _job.ExecutarAsync(CancellationToken.None);
    }
}
