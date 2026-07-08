using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DadosComprovanteEndereco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bairro",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Cep",
                table: "IdentificacoesExtraidas",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Cidade",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Complemento",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataVencimento",
                table: "IdentificacoesExtraidas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmissorDocumento",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Logradouro",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MesReferencia",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NomeTitularEndereco",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NumeroCliente",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NumeroEndereco",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NumeroInstalacao",
                table: "IdentificacoesExtraidas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_IdentificacoesExtraidas_Cep",
                table: "IdentificacoesExtraidas",
                column: "Cep");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdentificacoesExtraidas_Cep",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "Bairro",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "Cep",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "Cidade",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "Complemento",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "DataVencimento",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "EmissorDocumento",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "Logradouro",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "MesReferencia",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "NomeTitularEndereco",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "NumeroCliente",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "NumeroEndereco",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "NumeroInstalacao",
                table: "IdentificacoesExtraidas");
        }
    }
}
