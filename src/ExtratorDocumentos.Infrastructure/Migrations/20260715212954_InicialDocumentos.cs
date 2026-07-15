using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExtratorDocumentos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InicialDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Cpf = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CpfDependente = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cnpj = table.Column<string>(type: "varchar(14)", maxLength: 14, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomeOriginal = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HashSha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Extensao = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MimeType = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    Papel = table.Column<int>(type: "int", nullable: false),
                    TipoParentesco = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Observacoes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VersaoAtual = table.Column<int>(type: "int", nullable: false),
                    Excluido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExcluidoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    StatusExtracao = table.Column<int>(type: "int", nullable: false),
                    ErroExtracao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExtraidoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TipoDocumentoIdentificado = table.Column<int>(type: "int", nullable: true),
                    ConfiancaIdentificacao = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    ResultadoIdentificacaoJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultadoExtracaoJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModeloIdentificacao = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModeloExtracao = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VersaoExtrator = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VersaoSchema = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProcessadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ErroProcessamento = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                    Descricao = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
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
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Permitido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Bloqueado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PrioridadeIdentificacao = table.Column<int>(type: "int", nullable: false),
                    PrioridadeExtracao = table.Column<int>(type: "int", nullable: false),
                    PrimeiroVistoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UltimoVistoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenRouterModelos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DocumentoExtracoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TipoDocumentoSolicitado = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoDocumentoIdentificado = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VersaoSchema = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VersaoExtrator = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Confianca = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ResultadoJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModeloIdentificacao = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModeloExtracao = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErroCodigo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErroMensagem = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcessadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoExtracoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentoExtracoes_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DocumentoHistoricos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Acao = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StatusAnterior = table.Column<int>(type: "int", nullable: true),
                    StatusNovo = table.Column<int>(type: "int", nullable: true),
                    Observacao = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Usuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Data = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoHistoricos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentoHistoricos_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DocumentoVersoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Versao = table.Column<int>(type: "int", nullable: false),
                    NomeArquivo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoConteudo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    HashSha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChaveStorage = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoVersoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentoVersoes_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UsosOpenRouter",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DocumentoExtracaoId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
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
                        name: "FK_UsosOpenRouter_DocumentoExtracoes_DocumentoExtracaoId",
                        column: x => x.DocumentoExtracaoId,
                        principalTable: "DocumentoExtracoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UsosOpenRouter_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoExtracoes_DocumentoId_TipoDocumentoSolicitado_Versa~",
                table: "DocumentoExtracoes",
                columns: new[] { "DocumentoId", "TipoDocumentoSolicitado", "VersaoSchema", "VersaoExtrator" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoExtracoes_Status",
                table: "DocumentoExtracoes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoHistoricos_DocumentoId",
                table: "DocumentoHistoricos",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_Cnpj",
                table: "Documentos",
                column: "Cnpj");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_Cpf",
                table: "Documentos",
                column: "Cpf");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_CpfDependente",
                table: "Documentos",
                column: "CpfDependente");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_Excluido",
                table: "Documentos",
                column: "Excluido");

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
                name: "IX_Documentos_Status",
                table: "Documentos",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoVersoes_DocumentoId_Versao",
                table: "DocumentoVersoes",
                columns: new[] { "DocumentoId", "Versao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoVersoes_HashSha256",
                table: "DocumentoVersoes",
                column: "HashSha256");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterModelos_Ativo",
                table: "OpenRouterModelos",
                column: "Ativo");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterModelos_Bloqueado",
                table: "OpenRouterModelos",
                column: "Bloqueado");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterModelos_Disponivel",
                table: "OpenRouterModelos",
                column: "Disponivel");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterModelos_Permitido",
                table: "OpenRouterModelos",
                column: "Permitido");

            migrationBuilder.CreateIndex(
                name: "IX_OpenRouterModelos_UltimoVistoEm",
                table: "OpenRouterModelos",
                column: "UltimoVistoEm");

            migrationBuilder.CreateIndex(
                name: "IX_UsosOpenRouter_DocumentoExtracaoId",
                table: "UsosOpenRouter",
                column: "DocumentoExtracaoId");

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
                name: "DocumentoHistoricos");

            migrationBuilder.DropTable(
                name: "DocumentoVersoes");

            migrationBuilder.DropTable(
                name: "OpenRouterModelos");

            migrationBuilder.DropTable(
                name: "UsosOpenRouter");

            migrationBuilder.DropTable(
                name: "DocumentoExtracoes");

            migrationBuilder.DropTable(
                name: "Documentos");
        }
    }
}
