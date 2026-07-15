using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SchemaSugestaoConceitualCampos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Confianca",
                table: "TiposDocumentoCampos",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EncontradoNoArquivo",
                table: "TiposDocumentoCampos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ObrigatorioSugerido",
                table: "TiposDocumentoCampos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OrigemSugestao",
                table: "TiposDocumentoCampos",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "conhecimento_tipo")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confianca",
                table: "TiposDocumentoCampos");

            migrationBuilder.DropColumn(
                name: "EncontradoNoArquivo",
                table: "TiposDocumentoCampos");

            migrationBuilder.DropColumn(
                name: "ObrigatorioSugerido",
                table: "TiposDocumentoCampos");

            migrationBuilder.DropColumn(
                name: "OrigemSugestao",
                table: "TiposDocumentoCampos");
        }
    }
}
