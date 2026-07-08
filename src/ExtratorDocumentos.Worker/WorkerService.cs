using ExtratorDocumentos.Application.Jobs;

namespace ExtratorDocumentos.Worker
{
    public class WorkerService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<WorkerService> _logger;
        public WorkerService(IServiceProvider sp, ILogger<WorkerService> logger)
        {
            _sp = sp; _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<ProcessarDocumentosPendentesJob>()
                        .ExecutarAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during document processing");
                }
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
