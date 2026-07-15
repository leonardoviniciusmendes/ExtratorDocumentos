using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IdentificacaoUniversalExtracoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DocumentoExtracaoId",
                table: "UsosOpenRouter",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "DocumentoExtracoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TipoDocumentoSolicitado = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoDocumentoIdentificado = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VersaoSchema = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VersaoExtrator = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Confianca = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ResultadoJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModeloIdentificacao = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModeloExtracao = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErroCodigo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErroMensagem = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcessadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoExtracoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentoExtracoes_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UsosOpenRouter_DocumentoExtracaoId",
                table: "UsosOpenRouter",
                column: "DocumentoExtracaoId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoExtracoes_DocumentoId_TipoDocumentoSolicitado_Versa~",
                table: "DocumentoExtracoes",
                columns: new[] { "DocumentoId", "TipoDocumentoSolicitado", "VersaoSchema", "VersaoExtrator" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoExtracoes_Status",
                table: "DocumentoExtracoes",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_UsosOpenRouter_DocumentoExtracoes_DocumentoExtracaoId",
                table: "UsosOpenRouter",
                column: "DocumentoExtracaoId",
                principalTable: "DocumentoExtracoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsosOpenRouter_DocumentoExtracoes_DocumentoExtracaoId",
                table: "UsosOpenRouter");

            migrationBuilder.DropTable(
                name: "DocumentoExtracoes");

            migrationBuilder.DropIndex(
                name: "IX_UsosOpenRouter_DocumentoExtracaoId",
                table: "UsosOpenRouter");

            migrationBuilder.DropColumn(
                name: "DocumentoExtracaoId",
                table: "UsosOpenRouter");
        }
    }
}
