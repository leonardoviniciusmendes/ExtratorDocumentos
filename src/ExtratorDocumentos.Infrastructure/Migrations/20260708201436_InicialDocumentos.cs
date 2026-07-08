using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InicialDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClienteId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Observacoes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VersaoAtual = table.Column<int>(type: "int", nullable: false),
                    Excluido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExcluidoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DocumentoHistoricos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Acao = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StatusAnterior = table.Column<int>(type: "int", nullable: true),
                    StatusNovo = table.Column<int>(type: "int", nullable: true),
                    Observacao = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Usuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Data = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoHistoricos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentoHistoricos_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DocumentoVersoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Versao = table.Column<int>(type: "int", nullable: false),
                    NomeArquivo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoConteudo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    HashSha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChaveStorage = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoVersoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentoVersoes_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoHistoricos_DocumentoId",
                table: "DocumentoHistoricos",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_ClienteId",
                table: "Documentos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_Excluido",
                table: "Documentos",
                column: "Excluido");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_Status",
                table: "Documentos",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoVersoes_DocumentoId_Versao",
                table: "DocumentoVersoes",
                columns: new[] { "DocumentoId", "Versao" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentoHistoricos");

            migrationBuilder.DropTable(
                name: "DocumentoVersoes");

            migrationBuilder.DropTable(
                name: "Documentos");
        }
    }
}
