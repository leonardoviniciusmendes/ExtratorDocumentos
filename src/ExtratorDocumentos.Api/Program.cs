using System.Transactions;
using ExtratorDocumentos.Api.Services;
using ExtratorDocumentos.Application.Jobs;
using ExtratorDocumentos.Application.Services;
using ExtratorDocumentos.Application.Services.Extracao;
using ExtratorDocumentos.Infrastructure.Data;
using ExtratorDocumentos.Infrastructure.Repositories;
using ExtratorDocumentos.Infrastructure.Storage;
using Hangfire;
using Hangfire.MySql;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var conn = builder.Configuration.GetConnectionString("Default") ??
    "Server=localhost;Port=3320;Database=extrator_documentos;User=root;Password=ligado01;SslMode=None;AllowPublicKeyRetrieval=True;";
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(conn, new MySqlServerVersion(new Version(8, 0, 32))));

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseStorage(new MySqlStorage(conn, new MySqlStorageOptions
    {
        TransactionIsolationLevel = IsolationLevel.ReadCommitted,
        QueuePollInterval = TimeSpan.FromSeconds(15),
        PrepareSchemaIfNecessary = true,
        JobExpirationCheckInterval = TimeSpan.FromHours(1),
        CountersAggregateInterval = TimeSpan.FromMinutes(5),
        TransactionTimeout = TimeSpan.FromMinutes(1),
        TablesPrefix = "Hangfire"
    })));
builder.Services.AddHangfireServer();

builder.Services.AddScoped<DocumentoRepository>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<DocumentoService>();
builder.Services.AddHttpClient<OpenAiDocumentoExtracaoProvider>(client =>
{
    var baseUrl = builder.Configuration["DocumentExtraction:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
});
builder.Services.AddScoped<IDocumentoExtracaoProvider>(
    sp => sp.GetRequiredService<OpenAiDocumentoExtracaoProvider>());
builder.Services.AddHttpClient<OpenRouterDocumentoExtracaoProvider>(client =>
{
    var baseUrl = builder.Configuration["DocumentExtraction:OpenRouter:BaseUrl"]
        ?? "https://openrouter.ai/api/v1/";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
});
builder.Services.AddScoped<IDocumentoExtracaoProvider>(
    sp => sp.GetRequiredService<OpenRouterDocumentoExtracaoProvider>());
builder.Services.AddScoped<DocumentoExtracaoService>();
builder.Services.AddScoped<ProcessarDocumentosPendentesJob>();
builder.Services.AddScoped<HangfireJobScheduler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<HangfireJobScheduler>().RegistrarJobs();
}

BackgroundJob.Enqueue<HangfireJobScheduler>(
    scheduler => scheduler.ExecutarProcessamentoAsync());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();
