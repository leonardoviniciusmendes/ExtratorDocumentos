using System.Transactions;
using ExtratorDocumentos.Api.Services;
using ExtratorDocumentos.Application.Jobs;
using ExtratorDocumentos.Application.Services.Extracao;
using ExtratorDocumentos.Application.Services.Processamento;
using ExtratorDocumentos.Infrastructure.Data;
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

builder.Services.AddScoped<LocalStorageService>();
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
builder.Services.AddHttpClient<OpenRouterModelosService>(client =>
{
    var baseUrl = builder.Configuration["DocumentExtraction:OpenRouter:BaseUrl"]
        ?? "https://openrouter.ai/api/v1/";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
});
builder.Services.AddHttpClient<OpenRouterContaService>(client =>
{
    var baseUrl = builder.Configuration["DocumentExtraction:OpenRouter:BaseUrl"]
        ?? "https://openrouter.ai/api/v1/";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
});
builder.Services.AddScoped<IDocumentoExtracaoProvider>(
    sp => sp.GetRequiredService<OpenRouterDocumentoExtracaoProvider>());
builder.Services.AddScoped<IArquivoAnaliseService, ArquivoAnaliseService>();
builder.Services.AddScoped<OpenRouterModelSelector>();
builder.Services.AddScoped<IOpenRouterModelSelector>(
    sp => sp.GetRequiredService<OpenRouterModelSelector>());
builder.Services.AddScoped<IOpenRouterModelCandidateSelector>(
    sp => sp.GetRequiredService<OpenRouterModelSelector>());
builder.Services.AddScoped<IProcessadorDocumentoIdentificacaoService,
    ProcessadorDocumentoIdentificacaoService>();
builder.Services.AddScoped<IAssinaturaEstruturalDocumentoService,
    AssinaturaEstruturalDocumentoService>();
builder.Services.AddScoped<IIdentificadorTipoDocumentoService,
    IdentificadorTipoDocumentoService>();
builder.Services.AddScoped<IGeradorSchemaDocumentoService,
    GeradorSchemaDocumentoService>();
builder.Services.AddScoped<IAplicadorSchemaDocumentoService,
    AplicadorSchemaDocumentoService>();
builder.Services.AddScoped<ITipoDocumentoAprendizadoService,
    TipoDocumentoAprendizadoService>();
builder.Services.AddHttpClient<IOpenRouterPipelineClient, OpenRouterPipelineClient>(client =>
{
    var baseUrl = builder.Configuration["DocumentExtraction:OpenRouter:BaseUrl"]
        ?? "https://openrouter.ai/api/v1/";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
});
builder.Services.AddScoped<AtualizarOpenRouterModelosJob>();
builder.Services.AddScoped<HangfireJobScheduler>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
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

if (builder.Configuration.GetValue<bool?>("OpenRouter:AtualizacaoModelos:ExecutarAoIniciar") ??
    builder.Configuration.GetValue<bool?>("Jobs:OpenRouterModelos:ExecutarAoIniciar") ?? true)
{
    BackgroundJob.Enqueue<HangfireJobScheduler>(
        scheduler => scheduler.ExecutarAtualizacaoOpenRouterModelosAsync());
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();
