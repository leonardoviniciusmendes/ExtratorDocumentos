using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SepararIdentificacoesEnderecos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnderecosExtraidos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentoVersaoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Cpf = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cnpj = table.Column<string>(type: "varchar(14)", maxLength: 14, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cep = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Logradouro = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Numero = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Complemento = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Bairro = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cidade = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Uf = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FonteDocumento = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnderecosExtraidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnderecosExtraidos_DocumentoVersoes_DocumentoVersaoId",
                        column: x => x.DocumentoVersaoId,
                        principalTable: "DocumentoVersoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnderecosExtraidos_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EnderecosExtraidos_Cnpj",
                table: "EnderecosExtraidos",
                column: "Cnpj");

            migrationBuilder.CreateIndex(
                name: "IX_EnderecosExtraidos_Cpf",
                table: "EnderecosExtraidos",
                column: "Cpf");

            migrationBuilder.CreateIndex(
                name: "IX_EnderecosExtraidos_DocumentoId",
                table: "EnderecosExtraidos",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_EnderecosExtraidos_DocumentoVersaoId",
                table: "EnderecosExtraidos",
                column: "DocumentoVersaoId");

            migrationBuilder.Sql("""
                INSERT INTO EnderecosExtraidos
                    (Id, DocumentoId, DocumentoVersaoId, Cpf, Cnpj, Cep, Logradouro,
                     Numero, Complemento, Bairro, Cidade, Uf, FonteDocumento, CriadoEm)
                SELECT UUID(), DocumentoId, DocumentoVersaoId, Cpf, Cnpj, LEFT(Cep, 8),
                       Logradouro, NumeroEndereco, Complemento, Bairro, Cidade,
                       LEFT(Estado, 2), 'MigracaoIdentificacoesExtraidas', CriadoEm
                FROM IdentificacoesExtraidas
                WHERE Cep IS NOT NULL OR Logradouro IS NOT NULL OR NumeroEndereco IS NOT NULL
                   OR Complemento IS NOT NULL OR Bairro IS NOT NULL OR Cidade IS NOT NULL
                   OR Estado IS NOT NULL OR Endereco IS NOT NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_IdentificacoesExtraidas_Cep",
                table: "IdentificacoesExtraidas");

            migrationBuilder.DropColumn(name: "Bairro", table: "IdentificacoesExtraidas");
            migrationBuilder.DropColumn(name: "Cep", table: "IdentificacoesExtraidas");
            migrationBuilder.DropColumn(name: "Cidade", table: "IdentificacoesExtraidas");
            migrationBuilder.DropColumn(name: "Complemento", table: "IdentificacoesExtraidas");
            migrationBuilder.DropColumn(name: "Endereco", table: "IdentificacoesExtraidas");
            migrationBuilder.DropColumn(name: "Estado", table: "IdentificacoesExtraidas");
            migrationBuilder.DropColumn(name: "Logradouro", table: "IdentificacoesExtraidas");
            migrationBuilder.DropColumn(name: "NomeTitularEndereco", table: "IdentificacoesExtraidas");
            migrationBuilder.DropColumn(name: "NumeroEndereco", table: "IdentificacoesExtraidas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<string>(
                name: "Endereco",
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
                name: "NomeTitularEndereco",
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

            migrationBuilder.CreateIndex(
                name: "IX_IdentificacoesExtraidas_Cep",
                table: "IdentificacoesExtraidas",
                column: "Cep");

            migrationBuilder.Sql("""
                UPDATE IdentificacoesExtraidas i
                INNER JOIN EnderecosExtraidos e ON e.DocumentoId = i.DocumentoId
                SET i.Cep = e.Cep,
                    i.Logradouro = e.Logradouro,
                    i.NumeroEndereco = e.Numero,
                    i.Complemento = e.Complemento,
                    i.Bairro = e.Bairro,
                    i.Cidade = e.Cidade,
                    i.Estado = e.Uf;
                """);

            migrationBuilder.DropTable(
                name: "EnderecosExtraidos");
        }
    }
}
