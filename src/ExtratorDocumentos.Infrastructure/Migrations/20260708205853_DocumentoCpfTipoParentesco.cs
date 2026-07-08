using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentoCpfTipoParentesco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documentos_ClienteId",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Documentos");

            migrationBuilder.AddColumn<string>(
                name: "Cpf",
                table: "Documentos",
                type: "varchar(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TipoParentesco",
                table: "Documentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_Cpf",
                table: "Documentos",
                column: "Cpf");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documentos_Cpf",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Cpf",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "TipoParentesco",
                table: "Documentos");

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Documentos",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_ClienteId",
                table: "Documentos",
                column: "ClienteId");
        }
    }
}
