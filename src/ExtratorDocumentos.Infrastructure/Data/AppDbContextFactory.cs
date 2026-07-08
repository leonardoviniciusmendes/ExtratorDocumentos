using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExtratorDocumentos.Infrastructure.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        private const string DefaultConnectionString =
            "Server=localhost;Port=3320;Database=extrator_documentos;User=root;Password=ligado01;SslMode=None;AllowPublicKeyRetrieval=True;";

        public AppDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__Default") ??
                Environment.GetEnvironmentVariable("EXTRATOR_DOCUMENTOS_CONNECTION_STRING") ??
                DefaultConnectionString;

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 32)))
                .Options;
            return new AppDbContext(options);
        }
    }
}
