using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExtratorDocumentos.Infrastructure.Repositories
{
    public class DocumentoRepository
    {
        private readonly AppDbContext _db;
        public DocumentoRepository(AppDbContext db) { _db = db; }

        public async Task<Documento> AddAsync(Documento item, CancellationToken cancellationToken = default)
        {
            _db.Documentos.Add(item);
            await _db.SaveChangesAsync(cancellationToken);
            return item;
        }

        public Task<Documento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            _db.Documentos.Include(x => x.Versoes).Include(x => x.Historico)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task UpdateAsync(Documento item, CancellationToken cancellationToken = default)
        {
            _db.Documentos.Update(item);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
