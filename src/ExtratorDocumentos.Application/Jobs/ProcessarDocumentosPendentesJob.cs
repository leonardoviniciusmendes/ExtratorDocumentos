using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using ExtratorDocumentos.Infrastructure.Storage;
using ExtratorDocumentos.Application.Services.Extracao;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExtratorDocumentos.Application.Jobs
{
    public sealed class ProcessarDocumentosPendentesJob
    {
        private const string NomeLock = "extrator-documentos-processar-pendentes";
        private readonly AppDbContext _db;
        private readonly LocalStorageService _storage;
        private readonly ILogger<ProcessarDocumentosPendentesJob> _logger;
        private readonly DocumentoExtracaoService _extracaoService;

        public ProcessarDocumentosPendentesJob(AppDbContext db, LocalStorageService storage,
            DocumentoExtracaoService extracaoService,
            ILogger<ProcessarDocumentosPendentesJob> logger)
        {
            _db = db; _storage = storage; _extracaoService = extracaoService; _logger = logger;
        }

        public async Task<ProcessarDocumentosResult> ExecutarAsync(CancellationToken cancellationToken)
        {
            await _db.Database.OpenConnectionAsync(cancellationToken);
            var lockAdquirido = false;

            try
            {
                lockAdquirido = await TentarAdquirirLockAsync(cancellationToken);
                if (!lockAdquirido)
                {
                    _logger.LogInformation(
                        "Processamento ignorado porque outra instancia esta executando o job.");

                    return new ProcessarDocumentosResult(0, 0);
                }

                var documentos = await _db.Documentos
                    .Include(x => x.Versoes)
                    .Where(x => x.Status == StatusDocumento.Pendente && !x.Excluido)
                    .OrderBy(x => x.CriadoEm)
                    .Take(50)
                    .ToListAsync(cancellationToken);

                var processados = 0;
                var erros = 0;

                foreach (var documento in documentos)
                {
                    var versao = documento.Versoes
                        .FirstOrDefault(x => x.Versao == documento.VersaoAtual);

                    if (versao is null)
                    {
                        var statusAnteriorSemVersao = documento.Status;

                        documento.Status = StatusDocumento.Erro;
                        documento.AtualizadoEm = DateTime.UtcNow;

                        _db.DocumentoHistoricos.Add(new DocumentoHistorico
                        {
                            DocumentoId = documento.Id,
                            Acao = "Processamento",
                            StatusAnterior = statusAnteriorSemVersao,
                            StatusNovo = documento.Status
                        });

                        erros++;
                        continue;
                    }

                    var statusAnterior = documento.Status;

                    if (await _storage.ExisteAsync(versao.ChaveStorage, cancellationToken))
                    {
                        documento.Status = StatusDocumento.Disponivel;
                        processados++;
                    }
                    else
                    {
                        documento.Status = StatusDocumento.Erro;
                        erros++;
                    }

                    documento.AtualizadoEm = DateTime.UtcNow;

                    _db.DocumentoHistoricos.Add(new DocumentoHistorico
                    {
                        DocumentoId = documento.Id,
                        Acao = "Processamento",
                        StatusAnterior = statusAnterior,
                        StatusNovo = documento.Status
                    });
                }

                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex,
                        "Documento foi alterado por outro processo antes de salvar. Ignorando este ciclo.");

                    return new ProcessarDocumentosResult(processados, erros);
                }

                var documentosDisponiveisIds = documentos
                    .Where(x => x.Status == StatusDocumento.Disponivel)
                    .Select(x => x.Id)
                    .ToList();

                _db.ChangeTracker.Clear();

                foreach (var documentoId in documentosDisponiveisIds)
                {
                    await _extracaoService.ExtrairAsync(documentoId, cancellationToken);
                }

                _logger.LogInformation(
                    "Processamento finalizado. Processados: {Processados}; Erros: {Erros}",
                    processados,
                    erros);

                return new ProcessarDocumentosResult(processados, erros);
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

    public sealed record ProcessarDocumentosResult(int Processados, int Erros);
}
