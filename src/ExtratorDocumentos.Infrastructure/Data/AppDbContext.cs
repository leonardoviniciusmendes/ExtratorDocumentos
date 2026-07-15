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
        public DbSet<OpenRouterModelo> OpenRouterModelos { get; set; } = null!;
        public DbSet<UsoOpenRouter> UsosOpenRouter { get; set; } = null!;
        public DbSet<DocumentoExtracao> DocumentoExtracoes { get; set; } = null!;
        public DbSet<TipoDocumento> TiposDocumento { get; set; } = null!;
        public DbSet<TipoDocumentoSchema> TipoDocumentoSchemas { get; set; } = null!;
        public DbSet<TipoDocumentoCampo> TipoDocumentoCampos { get; set; } = null!;
        public DbSet<TipoDocumentoCampoSugerido> TipoDocumentoCamposSugeridos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Documento>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Cpf).HasMaxLength(11);
                b.Property(x => x.CpfDependente).HasMaxLength(11);
                b.Property(x => x.Cnpj).HasMaxLength(14);
                b.Property(x => x.NomeOriginal).IsRequired().HasMaxLength(255);
                b.Property(x => x.HashSha256).IsRequired().HasMaxLength(64);
                b.Property(x => x.Extensao).IsRequired().HasMaxLength(20);
                b.Property(x => x.MimeType).IsRequired().HasMaxLength(150);
                b.Property(x => x.ResultadoIdentificacaoJson).HasColumnType("longtext");
                b.Property(x => x.ResultadoExtracaoJson).HasColumnType("longtext");
                b.Property(x => x.ModeloIdentificacao).HasMaxLength(200);
                b.Property(x => x.ModeloExtracao).HasMaxLength(200);
                b.Property(x => x.VersaoExtrator).IsRequired().HasMaxLength(50);
                b.Property(x => x.VersaoSchema).IsRequired().HasMaxLength(50);
                b.Property(x => x.ErroProcessamento).HasMaxLength(4000);
                b.HasIndex(x => x.Cpf);
                b.HasIndex(x => x.CpfDependente);
                b.HasIndex(x => x.Cnpj);
                b.HasIndex(x => x.HashSha256).IsUnique();
                b.HasIndex(x => new { x.HashSha256, x.VersaoExtrator, x.VersaoSchema });
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
                b.HasIndex(x => x.HashSha256);
                b.HasOne(x => x.Documento).WithMany(x => x.Versoes).HasForeignKey(x => x.DocumentoId);
            });

            modelBuilder.Entity<DocumentoHistorico>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Acao).IsRequired().HasMaxLength(100);
                b.Property(x => x.Observacao).HasMaxLength(2000);
                b.HasIndex(x => x.DocumentoId);
                b.HasOne(x => x.Documento).WithMany(x => x.Historico).HasForeignKey(x => x.DocumentoId);
            });

            modelBuilder.Entity<OpenRouterModelo>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).HasMaxLength(200);
                b.Property(x => x.Nome).IsRequired().HasMaxLength(300);
                b.Property(x => x.Objetivo).IsRequired().HasMaxLength(1000);
                b.Property(x => x.Descricao).HasMaxLength(2000);
                b.Property(x => x.PrecoEntradaPorMilhaoTokens).HasPrecision(18, 8);
                b.Property(x => x.PrecoSaidaPorMilhaoTokens).HasPrecision(18, 8);
                b.HasIndex(x => x.Ativo);
                b.HasIndex(x => x.Disponivel);
                b.HasIndex(x => x.Permitido);
                b.HasIndex(x => x.Bloqueado);
                b.HasIndex(x => x.UltimoVistoEm);
            });

            modelBuilder.Entity<UsoOpenRouter>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ModeloId).IsRequired().HasMaxLength(200);
                b.Property(x => x.Objetivo).IsRequired().HasMaxLength(50);
                b.Property(x => x.Custo).HasPrecision(18, 8);
                b.Property(x => x.Erro).HasMaxLength(4000);
                b.Property(x => x.RequestId).HasMaxLength(200);
                b.HasIndex(x => x.DocumentoId);
                b.HasIndex(x => x.ModeloId);
                b.HasIndex(x => x.Objetivo);
                b.HasOne(x => x.Documento).WithMany(x => x.UsosOpenRouter)
                    .HasForeignKey(x => x.DocumentoId);
                b.HasOne(x => x.DocumentoExtracao).WithMany(x => x.UsosOpenRouter)
                    .HasForeignKey(x => x.DocumentoExtracaoId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<DocumentoExtracao>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.TipoDocumentoSolicitado).IsRequired().HasMaxLength(100);
                b.Property(x => x.TipoDocumentoIdentificado).HasMaxLength(100);
                b.Property(x => x.VersaoSchema).IsRequired().HasMaxLength(50);
                b.Property(x => x.VersaoExtrator).IsRequired().HasMaxLength(50);
                b.Property(x => x.Confianca).HasPrecision(5, 2);
                b.Property(x => x.SimilaridadeTipo).HasPrecision(5, 4);
                b.Property(x => x.ResultadoJson).HasColumnType("longtext");
                b.Property(x => x.ModeloIdentificacao).HasMaxLength(200);
                b.Property(x => x.ModeloExtracao).HasMaxLength(200);
                b.Property(x => x.ErroCodigo).HasMaxLength(100);
                b.Property(x => x.ErroMensagem).HasMaxLength(4000);
                b.HasIndex(x => new
                {
                    x.DocumentoId,
                    x.TipoDocumentoId,
                    x.TipoDocumentoSchemaId,
                    x.VersaoExtrator
                }).IsUnique();
                b.HasIndex(x => new
                {
                    x.DocumentoId,
                    x.TipoDocumentoSolicitado,
                    x.VersaoSchema,
                    x.VersaoExtrator
                });
                b.HasIndex(x => x.Status);
                b.HasOne(x => x.Documento).WithMany(x => x.Extracoes)
                    .HasForeignKey(x => x.DocumentoId);
                b.HasOne(x => x.TipoDocumento).WithMany(x => x.Extracoes)
                    .HasForeignKey(x => x.TipoDocumentoId)
                    .OnDelete(DeleteBehavior.SetNull);
                b.HasOne(x => x.TipoDocumentoSchema).WithMany(x => x.Extracoes)
                    .HasForeignKey(x => x.TipoDocumentoSchemaId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<TipoDocumento>(b =>
            {
                b.ToTable("TiposDocumento");
                b.HasKey(x => x.Id);
                b.Property(x => x.Codigo).IsRequired().HasMaxLength(150);
                b.Property(x => x.Nome).IsRequired().HasMaxLength(300);
                b.Property(x => x.Descricao).HasMaxLength(2000);
                b.Property(x => x.AssinaturaEstruturalJson).HasColumnType("longtext");
                b.HasIndex(x => x.Codigo).IsUnique();
                b.HasIndex(x => x.Ativo);
                b.HasIndex(x => x.Confirmado);
            });

            modelBuilder.Entity<TipoDocumentoSchema>(b =>
            {
                b.ToTable("TiposDocumentoSchemas");
                b.HasKey(x => x.Id);
                b.Property(x => x.Versao).IsRequired().HasMaxLength(50);
                b.Property(x => x.SchemaJson).IsRequired().HasColumnType("longtext");
                b.HasIndex(x => new { x.TipoDocumentoId, x.Versao }).IsUnique();
                b.HasIndex(x => new { x.TipoDocumentoId, x.Ativo });
                b.HasIndex(x => x.Status);
                b.HasOne(x => x.TipoDocumento).WithMany(x => x.Schemas)
                    .HasForeignKey(x => x.TipoDocumentoId);
                b.HasOne(x => x.DocumentoReferencia)
                    .WithMany()
                    .HasForeignKey(x => x.DocumentoReferenciaId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<TipoDocumentoCampo>(b =>
            {
                b.ToTable("TiposDocumentoCampos");
                b.HasKey(x => x.Id);
                b.Property(x => x.Chave).IsRequired().HasMaxLength(150);
                b.Property(x => x.NomeExibicao).IsRequired().HasMaxLength(300);
                b.Property(x => x.TipoDado).IsRequired().HasMaxLength(30);
                b.Property(x => x.ItemTipoDado).HasMaxLength(30);
                b.Property(x => x.OrigemSugestao).IsRequired().HasMaxLength(50)
                    .HasDefaultValue("conhecimento_tipo");
                b.Property(x => x.Confianca).HasPrecision(5, 4);
                b.Property(x => x.AliasesJson).HasColumnType("longtext");
                b.Property(x => x.RegraNormalizacao).HasMaxLength(100);
                b.Property(x => x.RegraValidacao).HasMaxLength(1000);
                b.Property(x => x.Descricao).HasMaxLength(1000);
                b.HasIndex(x => new { x.TipoDocumentoSchemaId, x.Chave }).IsUnique();
                b.HasIndex(x => x.CampoPaiId);
                b.HasOne(x => x.Schema).WithMany(x => x.Campos)
                    .HasForeignKey(x => x.TipoDocumentoSchemaId);
                b.HasOne(x => x.CampoPai).WithMany(x => x.CamposFilhos)
                    .HasForeignKey(x => x.CampoPaiId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TipoDocumentoCampoSugerido>(b =>
            {
                b.ToTable("TiposDocumentoCamposSugeridos");
                b.HasKey(x => x.Id);
                b.Property(x => x.Chave).IsRequired().HasMaxLength(150);
                b.Property(x => x.TipoDado).IsRequired().HasMaxLength(30);
                b.HasIndex(x => new { x.TipoDocumentoId, x.Chave }).IsUnique();
                b.HasIndex(x => x.Aprovado);
                b.HasOne(x => x.TipoDocumento).WithMany(x => x.CamposSugeridos)
                    .HasForeignKey(x => x.TipoDocumentoId);
            });
        }
    }
}
