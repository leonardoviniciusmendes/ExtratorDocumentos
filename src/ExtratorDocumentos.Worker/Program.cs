using ExtratorDocumentos.Application.Services.Extracao;
using ExtratorDocumentos.Infrastructure.Data;
using ExtratorDocumentos.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var conn = ctx.Configuration.GetConnectionString("Default") ??
            "Server=localhost;Port=3320;Database=extrator_documentos;User=root;Password=ligado01;SslMode=None;AllowPublicKeyRetrieval=True;";
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseMySql(conn, new MySqlServerVersion(new Version(8, 0, 32))));
        services.AddScoped<LocalStorageService>();
        services.AddHttpClient<OpenAiDocumentoExtracaoProvider>(client =>
        {
            var baseUrl = ctx.Configuration["DocumentExtraction:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        });
        services.AddScoped<IDocumentoExtracaoProvider>(
            sp => sp.GetRequiredService<OpenAiDocumentoExtracaoProvider>());
        services.AddHttpClient<OpenRouterDocumentoExtracaoProvider>(client =>
        {
            var baseUrl = ctx.Configuration["DocumentExtraction:OpenRouter:BaseUrl"]
                ?? "https://openrouter.ai/api/v1/";
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        });
        services.AddScoped<IDocumentoExtracaoProvider>(
            sp => sp.GetRequiredService<OpenRouterDocumentoExtracaoProvider>());
    });

await builder.RunConsoleAsync();
