using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DadosCompletosCnh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataEmissao",
                table: "IdentificacoesExtraidas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataPrimeiraHabilitacao",
                table: "IdentificacoesExtraidas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalEmissao",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NumeroRenach",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ObservacoesCnh",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataEmissao",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "DataPrimeiraHabilitacao",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "LocalEmissao",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "NumeroRenach",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "ObservacoesCnh",
                table: "IdentificacoesExtraidas");
        }
    }
}
