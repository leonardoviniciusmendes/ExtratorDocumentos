using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OpenRouterModelosCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpenRouterModelos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nome = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Objetivo = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContextoTokens = table.Column<int>(type: "int", nullable: true),
                    AceitaArquivo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AceitaImagem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SaidaTexto = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SuportaJson = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SuportaStructuredOutputs = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PrecoEntradaPorMilhaoTokens = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    PrecoSaidaPorMilhaoTokens = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    Disponivel = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PrimeiroVistoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UltimoVistoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenRouterModelos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterModelos_Disponivel",
                table: "OpenRouterModelos",
                column: "Disponivel");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterModelos_UltimoVistoEm",
                table: "OpenRouterModelos",
                column: "UltimoVistoEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpenRouterModelos");
        }
    }
}
