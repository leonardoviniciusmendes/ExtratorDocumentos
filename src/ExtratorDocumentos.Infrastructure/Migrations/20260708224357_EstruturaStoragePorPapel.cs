using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EstruturaStoragePorPapel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cnpj",
                table: "Documentos",
                type: "varchar(14)",
                maxLength: 14,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CpfDependente",
                table: "Documentos",
                type: "varchar(11)",
                maxLength: 11,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Papel",
                table: "Documentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_Cnpj",
                table: "Documentos",
                column: "Cnpj");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_CpfDependente",
                table: "Documentos",
                column: "CpfDependente");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documentos_Cnpj",
                table: "Documentos");

            migrationBuilder.DropIndex(
                name: "IX_Documentos_CpfDependente",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Cnpj",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "CpfDependente",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Papel",
                table: "Documentos");
        }
    }
}
