using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SchemasRascunhoPublicacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AtualizadoEm",
                table: "TiposDocumentoSchemas",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentoReferenciaId",
                table: "TiposDocumentoSchemas",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicadoEm",
                table: "TiposDocumentoSchemas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TiposDocumentoSchemas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CampoPaiId",
                table: "TiposDocumentoCampos",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "TiposDocumentoCampos",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ItemTipoDado",
                table: "TiposDocumentoCampos",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumentoSchemas_DocumentoReferenciaId",
                table: "TiposDocumentoSchemas",
                column: "DocumentoReferenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumentoSchemas_Status",
                table: "TiposDocumentoSchemas",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumentoCampos_CampoPaiId",
                table: "TiposDocumentoCampos",
                column: "CampoPaiId");

            migrationBuilder.AddForeignKey(
                name: "FK_TiposDocumentoCampos_TiposDocumentoCampos_CampoPaiId",
                table: "TiposDocumentoCampos",
                column: "CampoPaiId",
                principalTable: "TiposDocumentoCampos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TiposDocumentoSchemas_Documentos_DocumentoReferenciaId",
                table: "TiposDocumentoSchemas",
                column: "DocumentoReferenciaId",
                principalTable: "Documentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TiposDocumentoCampos_TiposDocumentoCampos_CampoPaiId",
                table: "TiposDocumentoCampos");

            migrationBuilder.DropForeignKey(
                name: "FK_TiposDocumentoSchemas_Documentos_DocumentoReferenciaId",
                table: "TiposDocumentoSchemas");

            migrationBuilder.DropIndex(
                name: "IX_TiposDocumentoSchemas_DocumentoReferenciaId",
                table: "TiposDocumentoSchemas");

            migrationBuilder.DropIndex(
                name: "IX_TiposDocumentoSchemas_Status",
                table: "TiposDocumentoSchemas");

            migrationBuilder.DropIndex(
                name: "IX_TiposDocumentoCampos_CampoPaiId",
                table: "TiposDocumentoCampos");

            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "TiposDocumentoSchemas");

            migrationBuilder.DropColumn(
                name: "DocumentoReferenciaId",
                table: "TiposDocumentoSchemas");

            migrationBuilder.DropColumn(
                name: "PublicadoEm",
                table: "TiposDocumentoSchemas");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TiposDocumentoSchemas");

            migrationBuilder.DropColumn(
                name: "CampoPaiId",
                table: "TiposDocumentoCampos");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "TiposDocumentoCampos");

            migrationBuilder.DropColumn(
                name: "ItemTipoDado",
                table: "TiposDocumentoCampos");
        }
    }
}
