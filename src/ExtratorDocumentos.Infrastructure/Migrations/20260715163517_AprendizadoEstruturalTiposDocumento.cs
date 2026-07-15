using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AprendizadoEstruturalTiposDocumento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DocumentoExtracoes_DocumentoId_FK_Temp",
                table: "DocumentoExtracoes",
                column: "DocumentoId");

            migrationBuilder.DropIndex(
                name: "IX_DocumentoExtracoes_DocumentoId_TipoDocumentoSolicitado_Versa~",
                table: "DocumentoExtracoes");

            migrationBuilder.AddColumn<decimal>(
                name: "SimilaridadeTipo",
                table: "DocumentoExtracoes",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TipoDocumentoId",
                table: "DocumentoExtracoes",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "TipoDocumentoSchemaId",
                table: "DocumentoExtracoes",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<bool>(
                name: "TipoReutilizado",
                table: "DocumentoExtracoes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TiposDocumento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Codigo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nome = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Confirmado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AssinaturaEstruturalJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposDocumento", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TiposDocumentoCamposSugeridos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TipoDocumentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Chave = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoDado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuantidadeOcorrencias = table.Column<int>(type: "int", nullable: false),
                    Aprovado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PrimeiraOcorrenciaEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UltimaOcorrenciaEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposDocumentoCamposSugeridos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiposDocumentoCamposSugeridos_TiposDocumento_TipoDocumentoId",
                        column: x => x.TipoDocumentoId,
                        principalTable: "TiposDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TiposDocumentoSchemas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TipoDocumentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Versao = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SchemaJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GeradoAutomaticamente = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposDocumentoSchemas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiposDocumentoSchemas_TiposDocumento_TipoDocumentoId",
                        column: x => x.TipoDocumentoId,
                        principalTable: "TiposDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TiposDocumentoCampos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TipoDocumentoSchemaId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Chave = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomeExibicao = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoDado = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Obrigatorio = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AliasesJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RegraNormalizacao = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RegraValidacao = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ordem = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposDocumentoCampos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiposDocumentoCampos_TiposDocumentoSchemas_TipoDocumentoSche~",
                        column: x => x.TipoDocumentoSchemaId,
                        principalTable: "TiposDocumentoSchemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoExtracoes_DocumentoId_TipoDocumentoId_TipoDocumento~",
                table: "DocumentoExtracoes",
                columns: new[] { "DocumentoId", "TipoDocumentoId", "TipoDocumentoSchemaId", "VersaoExtrator" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoExtracoes_DocumentoId_TipoDocumentoSolicitado_Versa~",
                table: "DocumentoExtracoes",
                columns: new[] { "DocumentoId", "TipoDocumentoSolicitado", "VersaoSchema", "VersaoExtrator" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoExtracoes_TipoDocumentoId",
                table: "DocumentoExtracoes",
                column: "TipoDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoExtracoes_TipoDocumentoSchemaId",
                table: "DocumentoExtracoes",
                column: "TipoDocumentoSchemaId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumento_Ativo",
                table: "TiposDocumento",
                column: "Ativo");

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumento_Codigo",
                table: "TiposDocumento",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumento_Confirmado",
                table: "TiposDocumento",
                column: "Confirmado");

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumentoCampos_TipoDocumentoSchemaId_Chave",
                table: "TiposDocumentoCampos",
                columns: new[] { "TipoDocumentoSchemaId", "Chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumentoCamposSugeridos_Aprovado",
                table: "TiposDocumentoCamposSugeridos",
                column: "Aprovado");

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumentoCamposSugeridos_TipoDocumentoId_Chave",
                table: "TiposDocumentoCamposSugeridos",
                columns: new[] { "TipoDocumentoId", "Chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumentoSchemas_TipoDocumentoId_Ativo",
                table: "TiposDocumentoSchemas",
                columns: new[] { "TipoDocumentoId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumentoSchemas_TipoDocumentoId_Versao",
                table: "TiposDocumentoSchemas",
                columns: new[] { "TipoDocumentoId", "Versao" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_DocumentoExtracoes_DocumentoId_FK_Temp",
                table: "DocumentoExtracoes");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentoExtracoes_TiposDocumentoSchemas_TipoDocumentoSchema~",
                table: "DocumentoExtracoes",
                column: "TipoDocumentoSchemaId",
                principalTable: "TiposDocumentoSchemas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentoExtracoes_TiposDocumento_TipoDocumentoId",
                table: "DocumentoExtracoes",
                column: "TipoDocumentoId",
                principalTable: "TiposDocumento",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DocumentoExtracoes_DocumentoId_FK_Temp",
                table: "DocumentoExtracoes",
                column: "DocumentoId");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentoExtracoes_TiposDocumentoSchemas_TipoDocumentoSchema~",
                table: "DocumentoExtracoes");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentoExtracoes_TiposDocumento_TipoDocumentoId",
                table: "DocumentoExtracoes");

            migrationBuilder.DropTable(
                name: "TiposDocumentoCampos");

            migrationBuilder.DropTable(
                name: "TiposDocumentoCamposSugeridos");

            migrationBuilder.DropTable(
                name: "TiposDocumentoSchemas");

            migrationBuilder.DropTable(
                name: "TiposDocumento");

            migrationBuilder.DropIndex(
                name: "IX_DocumentoExtracoes_DocumentoId_TipoDocumentoId_TipoDocumento~",
                table: "DocumentoExtracoes");

            migrationBuilder.DropIndex(
                name: "IX_DocumentoExtracoes_DocumentoId_TipoDocumentoSolicitado_Versa~",
                table: "DocumentoExtracoes");

            migrationBuilder.DropIndex(
                name: "IX_DocumentoExtracoes_TipoDocumentoId",
                table: "DocumentoExtracoes");

            migrationBuilder.DropIndex(
                name: "IX_DocumentoExtracoes_TipoDocumentoSchemaId",
                table: "DocumentoExtracoes");

            migrationBuilder.DropColumn(
                name: "SimilaridadeTipo",
                table: "DocumentoExtracoes");

            migrationBuilder.DropColumn(
                name: "TipoDocumentoId",
                table: "DocumentoExtracoes");

            migrationBuilder.DropColumn(
                name: "TipoDocumentoSchemaId",
                table: "DocumentoExtracoes");

            migrationBuilder.DropColumn(
                name: "TipoReutilizado",
                table: "DocumentoExtracoes");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoExtracoes_DocumentoId_TipoDocumentoSolicitado_Versa~",
                table: "DocumentoExtracoes",
                columns: new[] { "DocumentoId", "TipoDocumentoSolicitado", "VersaoSchema", "VersaoExtrator" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_DocumentoExtracoes_DocumentoId_FK_Temp",
                table: "DocumentoExtracoes");
        }
    }
}
