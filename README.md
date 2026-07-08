# ExtratorDocumentos

Biblioteca/serviço .NET para extração inteligente de dados de documentos brasileiros usando OCR, PDF parsing e IA via OpenRouter.

## Estrutura

- `ExtratorDocumentos.Api`: API REST, Swagger, Hangfire e health checks.
- `ExtratorDocumentos.Application`: serviços, DTOs, jobs e providers de extração.
- `ExtratorDocumentos.Domain`: entidades e enums do domínio.
- `ExtratorDocumentos.Infrastructure`: EF Core, migrations, repositórios e storage.
- `ExtratorDocumentos.Worker`: processamento assíncrono de documentos.

## Configuração

```powershell
$env:OPENROUTER_API_KEY="sua-chave"
```

O provider também aceita `DocumentExtraction__OpenRouter__ApiKey`.

## Executar

```powershell
dotnet restore src\ExtratorDocumentos.slnx
dotnet build src\ExtratorDocumentos.slnx
```

Com Docker:

```powershell
docker compose up --build -d
```

- API: `http://localhost:5001`
- Swagger: `http://localhost:5001/swagger`
- Health check: `http://localhost:5001/health`
- Hangfire: `http://localhost:5001/hangfire`
- MySQL: `localhost:3320`
