using ExtratorDocumentos.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExtratorDocumentos.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Documento> Documentos { get; set; } = null!;
        public DbSet<DocumentoVersao> DocumentoVersoes { get; set; } = null!;
        public DbSet<DocumentoHistorico> DocumentoHistoricos { get; set; } = null!;
        public DbSet<IdentificacaoExtraida> IdentificacoesExtraidas { get; set; } = null!;
        public DbSet<EnderecoExtraido> EnderecosExtraidos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Documento>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Cpf).IsRequired().HasMaxLength(11);
                b.Property(x => x.CpfDependente).HasMaxLength(11);
                b.Property(x => x.Cnpj).HasMaxLength(14);
                b.HasIndex(x => x.Cpf);
                b.HasIndex(x => x.CpfDependente);
                b.HasIndex(x => x.Cnpj);
                b.HasIndex(x => x.Status);
                b.HasIndex(x => x.Excluido);
                b.Property(x => x.Observacoes).HasMaxLength(2000);
            });

            modelBuilder.Entity<DocumentoVersao>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.NomeArquivo).IsRequired().HasMaxLength(255);
                b.Property(x => x.TipoConteudo).IsRequired().HasMaxLength(150);
                b.Property(x => x.HashSha256).IsRequired().HasMaxLength(64);
                b.Property(x => x.ChaveStorage).IsRequired().HasMaxLength(500);
                b.HasIndex(x => new { x.DocumentoId, x.Versao }).IsUnique();
                b.HasOne(x => x.Documento).WithMany(x => x.Versoes).HasForeignKey(x => x.DocumentoId);
            });

            modelBuilder.Entity<IdentificacaoExtraida>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.DocumentoId).IsUnique();
                b.HasIndex(x => x.DocumentoVersaoId);
                b.HasIndex(x => x.Cpf);
                b.HasIndex(x => x.Cnpj);
                b.Property(x => x.Confianca).HasPrecision(5, 2);
                b.Property(x => x.DadosBrutosJson).HasColumnType("longtext");
                b.HasOne(x => x.Documento).WithOne(x => x.Identificacao)
                    .HasForeignKey<IdentificacaoExtraida>(x => x.DocumentoId);
                b.HasOne(x => x.DocumentoVersao).WithMany().HasForeignKey(x => x.DocumentoVersaoId);
            });

            modelBuilder.Entity<EnderecoExtraido>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasIndex(x => x.Cpf);
                b.HasIndex(x => x.Cnpj);
                b.HasIndex(x => x.DocumentoId);
                b.HasIndex(x => x.DocumentoVersaoId);
                b.Property(x => x.Cpf).HasMaxLength(11);
                b.Property(x => x.Cnpj).HasMaxLength(14);
                b.Property(x => x.Cep).HasMaxLength(8);
                b.Property(x => x.Uf).HasMaxLength(2);
                b.Property(x => x.FonteDocumento).HasMaxLength(100);
                b.HasOne(x => x.Documento).WithMany(x => x.Enderecos)
                    .HasForeignKey(x => x.DocumentoId);
                b.HasOne(x => x.DocumentoVersao).WithMany()
                    .HasForeignKey(x => x.DocumentoVersaoId);
            });

            modelBuilder.Entity<DocumentoHistorico>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Acao).IsRequired().HasMaxLength(100);
                b.Property(x => x.Observacao).HasMaxLength(2000);
                b.HasIndex(x => x.DocumentoId);
                b.HasOne(x => x.Documento).WithMany(x => x.Historico).HasForeignKey(x => x.DocumentoId);
            });
        }
    }
}
