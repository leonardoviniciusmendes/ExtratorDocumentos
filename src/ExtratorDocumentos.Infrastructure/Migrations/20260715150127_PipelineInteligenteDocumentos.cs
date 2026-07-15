using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PipelineInteligenteDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "OpenRouterModelos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Bloqueado",
                table: "OpenRouterModelos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "OpenRouterModelos",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "Permitido",
                table: "OpenRouterModelos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "PrioridadeExtracao",
                table: "OpenRouterModelos",
                type: "int",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "PrioridadeIdentificacao",
                table: "OpenRouterModelos",
                type: "int",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<decimal>(
                name: "ConfiancaIdentificacao",
                table: "Documentos",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErroProcessamento",
                table: "Documentos",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Extensao",
                table: "Documentos",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "HashSha256",
                table: "Documentos",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "Documentos",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "application/octet-stream")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ModeloExtracao",
                table: "Documentos",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ModeloIdentificacao",
                table: "Documentos",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NomeOriginal",
                table: "Documentos",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessadoEm",
                table: "Documentos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultadoExtracaoJson",
                table: "Documentos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ResultadoIdentificacaoJson",
                table: "Documentos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "TamanhoBytes",
                table: "Documentos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "TipoDocumentoIdentificado",
                table: "Documentos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersaoExtrator",
                table: "Documentos",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "1")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VersaoSchema",
                table: "Documentos",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "1")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("""
                UPDATE Documentos d
                LEFT JOIN DocumentoVersoes v
                    ON v.DocumentoId = d.Id AND v.Versao = d.VersaoAtual
                SET
                    d.HashSha256 = COALESCE(NULLIF(v.HashSha256, ''), SHA2(CAST(d.Id AS CHAR), 256)),
                    d.NomeOriginal = COALESCE(NULLIF(v.NomeArquivo, ''), CONCAT(CAST(d.Id AS CHAR), '.bin')),
                    d.MimeType = COALESCE(NULLIF(v.TipoConteudo, ''), 'application/octet-stream'),
                    d.TamanhoBytes = COALESCE(v.TamanhoBytes, 0),
                    d.Extensao = CASE
                        WHEN v.NomeArquivo IS NULL OR LOCATE('.', v.NomeArquivo) = 0 THEN ''
                        ELSE CONCAT('.', LOWER(SUBSTRING_INDEX(v.NomeArquivo, '.', -1)))
                    END,
                    d.VersaoExtrator = '1',
                    d.VersaoSchema = '1'
                WHERE d.HashSha256 = '';
                """);

            migrationBuilder.CreateTable(
                name: "UsosOpenRouter",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ModeloId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Objetivo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TokensEntrada = table.Column<int>(type: "int", nullable: true),
                    TokensSaida = table.Column<int>(type: "int", nullable: true),
                    Custo = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    DuracaoMs = table.Column<long>(type: "bigint", nullable: false),
                    Sucesso = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Erro = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestId = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsosOpenRouter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsosOpenRouter_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterModelos_Ativo",
                table: "OpenRouterModelos",
                column: "Ativo");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterModelos_Bloqueado",
                table: "OpenRouterModelos",
                column: "Bloqueado");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterModelos_Permitido",
                table: "OpenRouterModelos",
                column: "Permitido");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_HashSha256",
                table: "Documentos",
                column: "HashSha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_HashSha256_VersaoExtrator_VersaoSchema",
                table: "Documentos",
                columns: new[] { "HashSha256", "VersaoExtrator", "VersaoSchema" });

            migrationBuilder.CreateIndex(
                name: "IX_UsosOpenRouter_DocumentoId",
                table: "UsosOpenRouter",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_UsosOpenRouter_ModeloId",
                table: "UsosOpenRouter",
                column: "ModeloId");

            migrationBuilder.CreateIndex(
                name: "IX_UsosOpenRouter_Objetivo",
                table: "UsosOpenRouter",
                column: "Objetivo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsosOpenRouter");

            migrationBuilder.DropIndex(
                name: "IX_OpenRouterModelos_Ativo",
                table: "OpenRouterModelos");

            migrationBuilder.DropIndex(
                name: "IX_OpenRouterModelos_Bloqueado",
                table: "OpenRouterModelos");

            migrationBuilder.DropIndex(
                name: "IX_OpenRouterModelos_Permitido",
                table: "OpenRouterModelos");

            migrationBuilder.DropIndex(
                name: "IX_Documentos_HashSha256",
                table: "Documentos");

            migrationBuilder.DropIndex(
                name: "IX_Documentos_HashSha256_VersaoExtrator_VersaoSchema",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "OpenRouterModelos");

            migrationBuilder.DropColumn(
                name: "Bloqueado",
                table: "OpenRouterModelos");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "OpenRouterModelos");

            migrationBuilder.DropColumn(
                name: "Permitido",
                table: "OpenRouterModelos");

            migrationBuilder.DropColumn(
                name: "PrioridadeExtracao",
                table: "OpenRouterModelos");

            migrationBuilder.DropColumn(
                name: "PrioridadeIdentificacao",
                table: "OpenRouterModelos");

            migrationBuilder.DropColumn(
                name: "ConfiancaIdentificacao",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "ErroProcessamento",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "Extensao",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "HashSha256",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "MimeType",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "ModeloExtracao",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "ModeloIdentificacao",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "NomeOriginal",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "ProcessadoEm",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "ResultadoExtracaoJson",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "ResultadoIdentificacaoJson",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "TamanhoBytes",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "TipoDocumentoIdentificado",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "VersaoExtrator",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "VersaoSchema",
                table: "Documentos");
        }
    }
}
