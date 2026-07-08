using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtracaoDadosIdentificacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErroExtracao",
                table: "Documentos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExtraidoEm",
                table: "Documentos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusExtracao",
                table: "Documentos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "IdentificacoesExtraidas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentoVersaoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    NomeCompleto = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cpf = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rg = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrgaoEmissor = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UfEmissao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataNascimento = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Naturalidade = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nacionalidade = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomeMae = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomePai = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumeroCnh = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CategoriaCnh = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidadeCnh = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Cnpj = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RazaoSocial = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomeFantasia = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Endereco = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MatriculaCertidao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Livro = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Folha = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Termo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Confianca = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Provedor = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DadosBrutosJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentificacoesExtraidas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentificacoesExtraidas_DocumentoVersoes_DocumentoVersaoId",
                        column: x => x.DocumentoVersaoId,
                        principalTable: "DocumentoVersoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IdentificacoesExtraidas_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_IdentificacoesExtraidas_Cnpj",
                table: "IdentificacoesExtraidas",
                column: "Cnpj");

            migrationBuilder.CreateIndex(
                name: "IX_IdentificacoesExtraidas_Cpf",
                table: "IdentificacoesExtraidas",
                column: "Cpf");

            migrationBuilder.CreateIndex(
                name: "IX_IdentificacoesExtraidas_DocumentoId",
                table: "IdentificacoesExtraidas",
                column: "DocumentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentificacoesExtraidas_DocumentoVersaoId",
                table: "IdentificacoesExtraidas",
                column: "DocumentoVersaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(
                name: "ErroExtracao",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "ExtraidoEm",
                table: "Documentos");

            migrationBuilder.DropColumn(
                name: "StatusExtracao",
                table: "Documentos");
        }
    }
}
