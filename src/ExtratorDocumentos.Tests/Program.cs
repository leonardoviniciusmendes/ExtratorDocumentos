using System.Text;
using System.Text.Json;
using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Application.Services.Processamento;
using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var tests = new (string Nome, Func<Task> Teste)[]
{
    ("arquivo texto suficiente nao exige visao", TextoSuficienteNaoExigeVisao),
    ("imagem exige modelo com visao", ImagemExigeVisao),
    ("pdf sem texto exige OCR", PdfSemTextoExigeOcr),
    ("pdf com paginas detecta contagem", PdfDetectaPaginas),
    ("csv indica possivel tabela", CsvIndicaTabela),
    ("dto universal mantem blocos obrigatorios", DtoUniversalMantemBlocos),
    ("campos inesperados preservados em camposExtraidos", CamposInesperadosPreservados),
    ("campos especificos aceitam valores flexiveis", CamposEspecificosFlexiveis),
    ("divergencia de tipo pode ser representada", DivergenciaTipoRepresentada),
    ("datas nulas permanecem nulas", DatasNulasPermanecemNulas),
    ("documento novo e versao ficam added", DocumentoNovoEVersaoFicamAdded),
    ("documento existente nao fica modified sem alteracao", DocumentoExistenteNaoFicaModified),
    ("documento extracao nao marca documento como modified", DocumentoExtracaoNaoMarcaDocumentoModified),
    ("modelo documento nao possui concurrency token", ModeloDocumentoNaoPossuiConcurrencyToken),
    ("indice hash documento permanece unico", IndiceHashDocumentoPermaneceUnico),
    ("indice composto extracao permanece unico", IndiceCompostoExtracaoPermaneceUnico),
    ("dbupdateconcurrency nao e violacao de unicidade", DbUpdateConcurrencyNaoEhViolacaoUnicidade)
};

var falhas = 0;
foreach (var (nome, teste) in tests)
{
    try
    {
        await teste();
        Console.WriteLine($"OK {nome}");
    }
    catch (Exception ex)
    {
        falhas++;
        Console.WriteLine($"FAIL {nome}: {ex.Message}");
    }
}

if (falhas > 0)
    Environment.Exit(1);

static async Task TextoSuficienteNaoExigeVisao()
{
    var service = new ArquivoAnaliseService();
    var bytes = Encoding.UTF8.GetBytes(new string('a', 600));
    var result = await service.AnalisarAsync("doc.txt", "text/plain", bytes, CancellationToken.None);
    Assert(result.PossuiTextoExtraivel);
    Assert(!result.ExigeModeloVisao);
}

static async Task ImagemExigeVisao()
{
    var service = new ArquivoAnaliseService();
    var result = await service.AnalisarAsync("foto.png", "image/png", [1, 2, 3], CancellationToken.None);
    Assert(result.PossuiImagens);
    Assert(result.ExigeModeloVisao);
}

static async Task PdfSemTextoExigeOcr()
{
    var service = new ArquivoAnaliseService();
    var bytes = Encoding.ASCII.GetBytes("%PDF-1.7\n/Type /Page\n/Image");
    var result = await service.AnalisarAsync("scan.pdf", "application/pdf", bytes, CancellationToken.None);
    Assert(result.NecessitaOcr);
    Assert(result.ExigeModeloVisao);
}

static async Task PdfDetectaPaginas()
{
    var service = new ArquivoAnaliseService();
    var bytes = Encoding.ASCII.GetBytes("%PDF\n/Type /Page\n/Type /Page\n");
    var result = await service.AnalisarAsync("doc.pdf", "application/pdf", bytes, CancellationToken.None);
    Assert(result.QuantidadePaginas == 2);
}

static async Task CsvIndicaTabela()
{
    var service = new ArquivoAnaliseService();
    var bytes = Encoding.UTF8.GetBytes("a;b;c\n1;2;3\n4;5;6\n" + new string('x', 100));
    var result = await service.AnalisarAsync("dados.csv", "text/csv", bytes, CancellationToken.None);
    Assert(result.PossuiTabelas);
}

static Task DtoUniversalMantemBlocos()
{
    var dto = new ResultadoIdentificacaoDocumentoDto();
    Assert(dto.Classificacao != null);
    Assert(dto.Titular != null);
    Assert(dto.Identificacao != null);
    Assert(dto.CamposEspecificos != null);
    Assert(dto.CamposExtraidos != null);
    Assert(dto.Validacao != null);
    Assert(dto.Processamento != null);
    return Task.CompletedTask;
}

static Task CamposInesperadosPreservados()
{
    var dto = new ResultadoIdentificacaoDocumentoDto();
    dto.CamposExtraidos.Add(new CampoExtraidoDocumentoDto
    {
        Chave = "numeroFiscal",
        RotuloOriginal = "Tax number",
        ValorOriginal = "ABC",
        ValorNormalizado = "ABC",
        TipoDado = "texto",
        Confianca = 0.8m
    });
    var json = JsonSerializer.Serialize(dto);
    Assert(json.Contains("numeroFiscal", StringComparison.Ordinal));
    return Task.CompletedTask;
}

static Task CamposEspecificosFlexiveis()
{
    var dto = new ResultadoIdentificacaoDocumentoDto();
    dto.CamposEspecificos.TipoDocumento = "passaporte";
    dto.CamposEspecificos.Valores["mrzValida"] = true;
    dto.CamposEspecificos.Valores["numeroPessoal"] = null;
    Assert(dto.CamposEspecificos.Valores.ContainsKey("mrzValida"));
    Assert(dto.CamposEspecificos.Valores.ContainsKey("numeroPessoal"));
    return Task.CompletedTask;
}

static Task DivergenciaTipoRepresentada()
{
    var dto = new ResultadoIdentificacaoDocumentoDto();
    dto.Classificacao.TipoInformado = "identificacao";
    dto.Classificacao.TipoIdentificado = "contrato";
    dto.Classificacao.TipoConfirmado = false;
    dto.Validacao.Divergencias.Add(new DivergenciaDocumentoDto
    {
        Campo = "tipoDocumento",
        ValorInformado = "identificacao",
        ValorIdentificado = "contrato",
        Mensagem = "O arquivo enviado aparenta ser contrato."
    });
    Assert(dto.Validacao.Divergencias.Count == 1);
    return Task.CompletedTask;
}

static Task DatasNulasPermanecemNulas()
{
    var dto = new ResultadoIdentificacaoDocumentoDto();
    dto.Identificacao.DataValidade = null;
    var json = JsonSerializer.Serialize(dto);
    Assert(json.Contains("\"dataValidade\":null", StringComparison.OrdinalIgnoreCase));
    return Task.CompletedTask;
}

static Task DocumentoNovoEVersaoFicamAdded()
{
    using var db = CriarDbContextSemProvider();
    var documento = CriarDocumento("hash-novo");
    var versao = new DocumentoVersao
    {
        DocumentoId = documento.Id,
        Versao = 1,
        NomeArquivo = "doc.pdf",
        TipoConteudo = documento.MimeType,
        TamanhoBytes = documento.TamanhoBytes,
        HashSha256 = documento.HashSha256,
        ChaveStorage = "pendentes/doc.pdf"
    };

    db.Documentos.Add(documento);
    db.DocumentoVersoes.Add(versao);

    Assert(db.Entry(documento).State == EntityState.Added);
    Assert(db.Entry(versao).State == EntityState.Added);
    return Task.CompletedTask;
}

static Task DocumentoExistenteNaoFicaModified()
{
    using var db = CriarDbContextSemProvider();
    var documento = CriarDocumento("hash-existente");

    db.Attach(documento);

    Assert(db.Entry(documento).State == EntityState.Unchanged);
    return Task.CompletedTask;
}

static Task DocumentoExtracaoNaoMarcaDocumentoModified()
{
    using var db = CriarDbContextSemProvider();
    var documento = CriarDocumento("hash-extracao");
    db.Attach(documento);

    db.DocumentoExtracoes.Add(new DocumentoExtracao
    {
        DocumentoId = documento.Id,
        TipoDocumentoSolicitado = "identificacao",
        VersaoSchema = "identificacao-1.0",
        VersaoExtrator = "1.0.0",
        Status = StatusExtracao.Pendente
    });

    Assert(db.Entry(documento).State == EntityState.Unchanged);
    return Task.CompletedTask;
}

static Task ModeloDocumentoNaoPossuiConcurrencyToken()
{
    using var db = CriarDbContextSemProvider();
    var entityType = db.Model.FindEntityType(typeof(Documento)) ??
        throw new InvalidOperationException("Documento nao mapeado.");

    Assert(!entityType.GetProperties().Any(x => x.IsConcurrencyToken));
    return Task.CompletedTask;
}

static Task IndiceHashDocumentoPermaneceUnico()
{
    using var db = CriarDbContextSemProvider();
    var entityType = db.Model.FindEntityType(typeof(Documento)) ??
        throw new InvalidOperationException("Documento nao mapeado.");
    var index = entityType.GetIndexes().FirstOrDefault(x =>
        x.Properties.Select(p => p.Name).SequenceEqual(["HashSha256"]));

    Assert(index?.IsUnique == true);
    return Task.CompletedTask;
}

static Task IndiceCompostoExtracaoPermaneceUnico()
{
    using var db = CriarDbContextSemProvider();
    var entityType = db.Model.FindEntityType(typeof(DocumentoExtracao)) ??
        throw new InvalidOperationException("DocumentoExtracao nao mapeado.");
    var index = entityType.GetIndexes().FirstOrDefault(x =>
        x.Properties.Select(p => p.Name).SequenceEqual([
            "DocumentoId",
            "TipoDocumentoSolicitado",
            "VersaoSchema",
            "VersaoExtrator"
        ]));

    Assert(index?.IsUnique == true);
    return Task.CompletedTask;
}

static Task DbUpdateConcurrencyNaoEhViolacaoUnicidade()
{
    var exception = new DbUpdateConcurrencyException("concorrencia otimista");
    Assert(!EhViolacaoUnicidade(exception));
    return Task.CompletedTask;
}

static AppDbContext CriarDbContextSemProvider() =>
    new(new DbContextOptionsBuilder<AppDbContext>()
        .UseMySql(
            "Server=localhost;Database=extrator_documentos_tests;User=root;Password=tests",
            new MySqlServerVersion(new Version(8, 0, 36)))
        .Options);

static Documento CriarDocumento(string hash) =>
    new()
    {
        NomeOriginal = "doc.pdf",
        HashSha256 = hash,
        Extensao = ".pdf",
        MimeType = "application/pdf",
        TamanhoBytes = 10,
        Tipo = TipoDocumento.Outros,
        Status = StatusDocumento.Pendente,
        VersaoSchema = "identificacao-1.0",
        VersaoExtrator = "1.0.0"
    };

static bool EhViolacaoUnicidade(Exception ex) =>
    ex is DbUpdateException updateException &&
    (updateException.InnerException?.Message.Contains("Duplicate",
        StringComparison.OrdinalIgnoreCase) == true ||
     updateException.InnerException?.Message.Contains("UNIQUE",
        StringComparison.OrdinalIgnoreCase) == true);

static void Assert(bool condition)
{
    if (!condition) throw new InvalidOperationException("Assertion failed.");
}
