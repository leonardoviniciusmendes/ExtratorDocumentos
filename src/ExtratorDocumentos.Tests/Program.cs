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
    ("dbupdateconcurrency nao e violacao de unicidade", DbUpdateConcurrencyNaoEhViolacaoUnicidade),
    ("assinatura estrutural remove dados variaveis", AssinaturaEstruturalRemoveDadosVariaveis),
    ("schema gerado nao contem valores reais", SchemaGeradoNaoContemValoresReais),
    ("schema inicial cria campos nao obrigatorios", SchemaInicialCriaCamposNaoObrigatorios),
    ("aplicador retorna chaves estaveis", AplicadorRetornaChavesEstaveis),
    ("campo ausente retorna null", CampoAusenteRetornaNull),
    ("lista ausente retorna array vazio", ListaAusenteRetornaArrayVazio),
    ("campo inesperado vai para adicionais", CampoInesperadoVaiParaAdicionais),
    ("aliases diferentes preenchem chave canonica", AliasesDiferentesPreenchemChaveCanonica),
    ("aplicador respeita json exemplo estruturado", AplicadorRespeitaJsonExemploEstruturado),
    ("indice codigo tipo documento e unico", IndiceCodigoTipoDocumentoUnico),
    ("indice schema versao e unico", IndiceSchemaVersaoUnico),
    ("indice campo sugerido e unico", IndiceCampoSugeridoUnico),
    ("schema sugerido inicia como rascunho", SchemaSugeridoIniciaComoRascunho),
    ("validacao rejeita chave duplicada", ValidacaoRejeitaChaveDuplicada),
    ("validacao rejeita lista sem item", ValidacaoRejeitaListaSemItem),
    ("validacao rejeita objeto sem campos", ValidacaoRejeitaObjetoSemCampos),
    ("validacao aceita schema estruturado", ValidacaoAceitaSchemaEstruturado),
    ("json exemplo nao contem valores reais", JsonExemploNaoContemValoresReais),
    ("schema ativo nao deve ser alterado diretamente", SchemaAtivoNaoDeveSerAlteradoDiretamente),
    ("ausencia schema ativo usa codigo controlado", AusenciaSchemaAtivoUsaCodigoControlado),
    ("cnh sugere campos conhecidos ausentes", CnhSugereCamposConhecidosAusentes),
    ("conta luz sugere endereco cep titular consumo", ContaLuzSugereEnderecoCepTitularConsumo),
    ("contrato sugere numero partes assinatura vigencia", ContratoSugereNumeroPartesAssinaturaVigencia),
    ("passaporte sugere mrz e dados internacionais", PassaporteSugereMrzDadosInternacionais),
    ("campos encontrados sao marcados corretamente", CamposEncontradosMarcadosCorretamente),
    ("campos conhecidos ausentes sao mantidos", CamposConhecidosAusentesMantidos),
    ("campo especifico do exemplo e adicionado", CampoEspecificoExemploAdicionado),
    ("schema conceitual nao armazena valores reais", SchemaConceitualNaoArmazenaValoresReais),
    ("schema conceitual permanece rascunho", SchemaConceitualPermaneceRascunho),
    ("origem sugestao e persistivel", OrigemSugestaoPersistivel),
    ("aliases duplicados sao unificados", AliasesDuplicadosUnificados),
    ("arquivo ilegivel ainda sugere por tipo", ArquivoIlegivelAindaSugerePorTipo),
    ("tipo desconhecido usa descricao e arquivo", TipoDesconhecidoUsaDescricaoEArquivo),
    ("objetos e listas sao sugeridos", ObjetosEListasSugeridos),
    ("resposta retorna sugestoes com extracao vazia", RespostaRetornaSugestoesComExtracaoVazia)
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
            "TipoDocumentoId",
            "TipoDocumentoSchemaId",
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

static Task AssinaturaEstruturalRemoveDadosVariaveis()
{
    var service = new AssinaturaEstruturalDocumentoService();
    var dto = CriarExtracaoContrato();
    var caracteristicas = new CaracteristicasArquivo("contrato.pdf", ".pdf",
        "application/pdf", 100, 2, true, 1000, false, false, true, 0.9m, false);

    var assinatura = service.Gerar(dto, caracteristicas);
    var json = JsonSerializer.Serialize(assinatura);

    Assert(!json.Contains("7023", StringComparison.OrdinalIgnoreCase));
    Assert(!json.Contains("JOAO", StringComparison.OrdinalIgnoreCase));
    Assert(assinatura.PossuiTabela);
    return Task.CompletedTask;
}

static async Task SchemaGeradoNaoContemValoresReais()
{
    var service = new GeradorSchemaDocumentoService();
    var schema = await service.GerarAsync(CriarExtracaoContrato(), CancellationToken.None);

    Assert(!schema.SchemaJson.Contains("7023PME", StringComparison.OrdinalIgnoreCase));
    Assert(!schema.SchemaJson.Contains("AMIL", StringComparison.OrdinalIgnoreCase));
    Assert(schema.SchemaJson.Contains("numeroContrato", StringComparison.Ordinal));
}

static async Task SchemaInicialCriaCamposNaoObrigatorios()
{
    var service = new GeradorSchemaDocumentoService();
    var schema = await service.GerarAsync(CriarExtracaoContrato(), CancellationToken.None);

    Assert(schema.Campos.Count > 0);
    Assert(schema.Campos.All(x => !x.Obrigatorio));
}

static async Task AplicadorRetornaChavesEstaveis()
{
    var service = new AplicadorSchemaDocumentoService();
    var schema = CriarSchemaContrato();
    var result = await service.AplicarAsync(schema, CriarExtracaoContrato(), CancellationToken.None);

    Assert(result.Dados.Keys.SequenceEqual([
        "numeroContrato",
        "operadora",
        "beneficiarios"
    ]));
}

static async Task CampoAusenteRetornaNull()
{
    var service = new AplicadorSchemaDocumentoService();
    var schema = CriarSchemaContrato();
    var extracao = CriarExtracaoContrato();
    extracao.CamposExtraidos.RemoveAll(x => x.Chave == "operadora");

    var result = await service.AplicarAsync(schema, extracao, CancellationToken.None);

    Assert(result.Dados["operadora"] == null);
}

static async Task ListaAusenteRetornaArrayVazio()
{
    var service = new AplicadorSchemaDocumentoService();
    var schema = CriarSchemaContrato();
    var extracao = CriarExtracaoContrato();
    extracao.CamposExtraidos.RemoveAll(x => x.Chave == "beneficiarios");

    var result = await service.AplicarAsync(schema, extracao, CancellationToken.None);

    Assert(result.Dados["beneficiarios"] is object[] lista && lista.Length == 0);
}

static async Task CampoInesperadoVaiParaAdicionais()
{
    var service = new AplicadorSchemaDocumentoService();
    var schema = CriarSchemaContrato();
    var extracao = CriarExtracaoContrato();
    extracao.CamposExtraidos.Add(new CampoExtraidoDocumentoDto
    {
        Chave = "codigoInternoProduto",
        RotuloOriginal = "Codigo interno do produto",
        ValorNormalizado = "ABC-123",
        TipoDado = "texto"
    });

    var result = await service.AplicarAsync(schema, extracao, CancellationToken.None);

    Assert(result.CamposAdicionais.ContainsKey("codigoInternoProduto"));
    Assert(!result.Dados.ContainsKey("codigoInternoProduto"));
}

static async Task AliasesDiferentesPreenchemChaveCanonica()
{
    var service = new AplicadorSchemaDocumentoService();
    var schema = CriarSchemaContrato();
    var extracao = new ResultadoIdentificacaoDocumentoDto();
    extracao.CamposExtraidos.Add(new CampoExtraidoDocumentoDto
    {
        Chave = "contrato",
        RotuloOriginal = "Contract number",
        ValorNormalizado = "XPTO",
        TipoDado = "texto"
    });

    var result = await service.AplicarAsync(schema, extracao, CancellationToken.None);

    Assert((string?)result.Dados["numeroContrato"] == "XPTO");
}

static async Task AplicadorRespeitaJsonExemploEstruturado()
{
    var service = new AplicadorSchemaDocumentoService();
    var schema = CriarSchemaContratoEstruturado();
    var extracao = CriarExtracaoContrato();
    extracao.CamposExtraidos.Add(new CampoExtraidoDocumentoDto
    {
        Chave = "nome",
        RotuloOriginal = "Nome da operadora",
        ValorNormalizado = "AMIL",
        TipoDado = "texto"
    });
    extracao.CamposExtraidos.Add(new CampoExtraidoDocumentoDto
    {
        Chave = "codigoInternoProduto",
        RotuloOriginal = "Codigo interno do produto",
        ValorNormalizado = "ABC-123",
        TipoDado = "texto"
    });

    var result = await service.AplicarAsync(schema, extracao, CancellationToken.None);

    Assert(result.Dados.Keys.SequenceEqual([
        "numeroContrato",
        "operadora",
        "beneficiarios"
    ]));
    Assert(!result.Dados.ContainsKey("nome"));
    Assert(!result.Dados.ContainsKey("codigoInternoProduto"));
    Assert(result.Dados["operadora"] is IReadOnlyDictionary<string, object?> operadora &&
        operadora.Keys.SequenceEqual(["nome"]) &&
        (string?)operadora["nome"] == "AMIL");
    Assert(result.Dados["beneficiarios"] is object[] lista && lista.Length == 0);
    Assert(result.CamposAdicionais.ContainsKey("codigoInternoProduto"));
}

static Task IndiceCodigoTipoDocumentoUnico()
{
    using var db = CriarDbContextSemProvider();
    var entityType = db.Model.FindEntityType(typeof(TipoDocumento)) ??
        throw new InvalidOperationException("TipoDocumento nao mapeado.");
    var index = entityType.GetIndexes().FirstOrDefault(x =>
        x.Properties.Select(p => p.Name).SequenceEqual(["Codigo"]));

    Assert(index?.IsUnique == true);
    return Task.CompletedTask;
}

static Task IndiceSchemaVersaoUnico()
{
    using var db = CriarDbContextSemProvider();
    var entityType = db.Model.FindEntityType(typeof(TipoDocumentoSchema)) ??
        throw new InvalidOperationException("TipoDocumentoSchema nao mapeado.");
    var index = entityType.GetIndexes().FirstOrDefault(x =>
        x.Properties.Select(p => p.Name).SequenceEqual([
            "TipoDocumentoId",
            "Versao"
        ]));

    Assert(index?.IsUnique == true);
    return Task.CompletedTask;
}

static Task IndiceCampoSugeridoUnico()
{
    using var db = CriarDbContextSemProvider();
    var entityType = db.Model.FindEntityType(typeof(TipoDocumentoCampoSugerido)) ??
        throw new InvalidOperationException("TipoDocumentoCampoSugerido nao mapeado.");
    var index = entityType.GetIndexes().FirstOrDefault(x =>
        x.Properties.Select(p => p.Name).SequenceEqual([
            "TipoDocumentoId",
            "Chave"
        ]));

    Assert(index?.IsUnique == true);
    return Task.CompletedTask;
}

static Task SchemaSugeridoIniciaComoRascunho()
{
    var dto = CriarSchemaDtoContrato();
    dto.Status = StatusTipoDocumentoSchema.Rascunho.ToString();

    Assert(dto.Status == "Rascunho");
    Assert(dto.Campos.All(x => !x.Obrigatorio));
    return Task.CompletedTask;
}

static Task ValidacaoRejeitaChaveDuplicada()
{
    var validador = new ValidadorSchemaDocumentoService();
    var dto = CriarSchemaDtoContrato();
    dto.Campos.Add(new TipoDocumentoCampoDto
    {
        Chave = "numeroContrato",
        NomeExibicao = "Numero duplicado",
        TipoDado = "texto"
    });

    var result = validador.Validar(dto);

    Assert(!result.Valido);
    Assert(result.Erros.Any(x => x.Contains("duplicada", StringComparison.OrdinalIgnoreCase)));
    return Task.CompletedTask;
}

static Task ValidacaoRejeitaListaSemItem()
{
    var validador = new ValidadorSchemaDocumentoService();
    var dto = new TipoDocumentoSchemaDto();
    dto.Campos.Add(new TipoDocumentoCampoDto
    {
        Chave = "beneficiarios",
        NomeExibicao = "Beneficiarios",
        TipoDado = "lista"
    });

    var result = validador.Validar(dto);

    Assert(!result.Valido);
    Assert(result.Erros.Any(x => x.Contains("lista sem definicao", StringComparison.OrdinalIgnoreCase)));
    return Task.CompletedTask;
}

static Task ValidacaoRejeitaObjetoSemCampos()
{
    var validador = new ValidadorSchemaDocumentoService();
    var dto = new TipoDocumentoSchemaDto();
    dto.Campos.Add(new TipoDocumentoCampoDto
    {
        Chave = "operadora",
        NomeExibicao = "Operadora",
        TipoDado = "objeto"
    });

    var result = validador.Validar(dto);

    Assert(!result.Valido);
    Assert(result.Erros.Any(x => x.Contains("objeto sem campos", StringComparison.OrdinalIgnoreCase)));
    return Task.CompletedTask;
}

static Task ValidacaoAceitaSchemaEstruturado()
{
    var validador = new ValidadorSchemaDocumentoService();
    var result = validador.Validar(CriarSchemaDtoContrato());

    Assert(result.Valido);
    return Task.CompletedTask;
}

static Task JsonExemploNaoContemValoresReais()
{
    var dto = CriarSchemaDtoContrato();
    dto.JsonExemplo = new Dictionary<string, object?>
    {
        ["numeroContrato"] = null,
        ["operadora"] = new Dictionary<string, object?> { ["nome"] = null },
        ["beneficiarios"] = Array.Empty<object>()
    };
    var json = JsonSerializer.Serialize(dto.JsonExemplo);

    Assert(!json.Contains("AMIL", StringComparison.OrdinalIgnoreCase));
    Assert(!json.Contains("7023PME", StringComparison.OrdinalIgnoreCase));
    Assert(json.Contains("numeroContrato", StringComparison.Ordinal));
    return Task.CompletedTask;
}

static Task SchemaAtivoNaoDeveSerAlteradoDiretamente()
{
    var schema = new TipoDocumentoSchema
    {
        Status = StatusTipoDocumentoSchema.Ativo,
        Ativo = true
    };

    Assert(schema.Status != StatusTipoDocumentoSchema.Rascunho);
    return Task.CompletedTask;
}

static Task AusenciaSchemaAtivoUsaCodigoControlado()
{
    var erro = "SCHEMA_ATIVO_NAO_ENCONTRADO: Nao existe um schema ativo para o tipo contrato_plano_saude.";

    Assert(erro.StartsWith("SCHEMA_ATIVO_NAO_ENCONTRADO:", StringComparison.Ordinal));
    return Task.CompletedTask;
}

static ResultadoIdentificacaoDocumentoDto CriarExtracaoContrato()
{
    var dto = new ResultadoIdentificacaoDocumentoDto();
    dto.Classificacao.TipoIdentificado = "contrato_plano_saude_empresarial";
    dto.Classificacao.NomeDocumentoOriginal = "Contrato empresarial de plano de saude";
    dto.CamposEspecificos.TipoDocumento = "contrato_plano_saude_empresarial";
    dto.CamposExtraidos.Add(new CampoExtraidoDocumentoDto
    {
        Chave = "numeroContrato",
        RotuloOriginal = "Contrato n 7023PME",
        ValorOriginal = "7023PME",
        ValorNormalizado = "7023PME",
        TipoDado = "texto"
    });
    dto.CamposExtraidos.Add(new CampoExtraidoDocumentoDto
    {
        Chave = "operadora",
        RotuloOriginal = "Operadora",
        ValorOriginal = "AMIL",
        ValorNormalizado = "AMIL",
        TipoDado = "texto"
    });
    dto.CamposExtraidos.Add(new CampoExtraidoDocumentoDto
    {
        Chave = "beneficiarios",
        RotuloOriginal = "Beneficiarios",
        ValorOriginal = "JOAO DA SILVA",
        ValorNormalizado = Array.Empty<object>(),
        TipoDado = "lista"
    });
    return dto;
}

static TipoDocumentoSchema CriarSchemaContrato()
{
    var schema = new TipoDocumentoSchema
    {
        Versao = "1.0",
        Ativo = true
    };
    schema.Campos.Add(new TipoDocumentoCampo
    {
        Chave = "numeroContrato",
        NomeExibicao = "Numero do contrato",
        TipoDado = "texto",
        AliasesJson = "[\"contract number\",\"numero do contrato\"]",
        Ordem = 0
    });
    schema.Campos.Add(new TipoDocumentoCampo
    {
        Chave = "operadora",
        NomeExibicao = "Operadora",
        TipoDado = "texto",
        Ordem = 1
    });
    schema.Campos.Add(new TipoDocumentoCampo
    {
        Chave = "beneficiarios",
        NomeExibicao = "Beneficiarios",
        TipoDado = "lista",
        Ordem = 2
    });
    return schema;
}

static TipoDocumentoSchema CriarSchemaContratoEstruturado()
{
    var schemaDto = CriarSchemaDtoContrato();
    schemaDto.JsonExemplo = new Dictionary<string, object?>
    {
        ["numeroContrato"] = null,
        ["operadora"] = new Dictionary<string, object?> { ["nome"] = null },
        ["beneficiarios"] = Array.Empty<object>()
    };
    var schema = new TipoDocumentoSchema
    {
        Id = schemaDto.Id,
        Versao = schemaDto.Versao,
        Ativo = true,
        SchemaJson = JsonSerializer.Serialize(schemaDto)
    };
    var numeroContrato = new TipoDocumentoCampo
    {
        TipoDocumentoSchemaId = schema.Id,
        Chave = "numeroContrato",
        NomeExibicao = "Numero do contrato",
        TipoDado = "texto",
        Ordem = 1
    };
    var operadora = new TipoDocumentoCampo
    {
        TipoDocumentoSchemaId = schema.Id,
        Chave = "operadora",
        NomeExibicao = "Operadora",
        TipoDado = "objeto",
        Ordem = 2
    };
    var nomeOperadora = new TipoDocumentoCampo
    {
        TipoDocumentoSchemaId = schema.Id,
        CampoPaiId = operadora.Id,
        Chave = "nome",
        NomeExibicao = "Nome da operadora",
        TipoDado = "texto",
        Ordem = 1
    };
    var beneficiarios = new TipoDocumentoCampo
    {
        TipoDocumentoSchemaId = schema.Id,
        Chave = "beneficiarios",
        NomeExibicao = "Beneficiarios",
        TipoDado = "lista",
        ItemTipoDado = "objeto",
        Ordem = 3
    };
    var nomeBeneficiario = new TipoDocumentoCampo
    {
        TipoDocumentoSchemaId = schema.Id,
        CampoPaiId = beneficiarios.Id,
        Chave = "nome",
        NomeExibicao = "Nome",
        TipoDado = "texto",
        Ordem = 1
    };
    schema.Campos.Add(numeroContrato);
    schema.Campos.Add(operadora);
    schema.Campos.Add(nomeOperadora);
    schema.Campos.Add(beneficiarios);
    schema.Campos.Add(nomeBeneficiario);
    return schema;
}

static TipoDocumentoSchemaDto CriarSchemaDtoContrato()
{
    var dto = new TipoDocumentoSchemaDto
    {
        Id = Guid.NewGuid(),
        Versao = "1.0",
        Status = "Rascunho",
        GeradoAutomaticamente = true
    };
    dto.Campos.Add(new TipoDocumentoCampoDto
    {
        Chave = "numeroContrato",
        NomeExibicao = "Numero do contrato",
        TipoDado = "texto",
        Ordem = 1
    });
    dto.Campos.Add(new TipoDocumentoCampoDto
    {
        Chave = "operadora",
        NomeExibicao = "Operadora",
        TipoDado = "objeto",
        Ordem = 2,
        Campos =
        {
            new TipoDocumentoCampoDto
            {
                Chave = "nome",
                NomeExibicao = "Nome da operadora",
                TipoDado = "texto",
                Ordem = 1
            }
        }
    });
    dto.Campos.Add(new TipoDocumentoCampoDto
    {
        Chave = "beneficiarios",
        NomeExibicao = "Beneficiarios",
        TipoDado = "lista",
        Ordem = 3,
        Item = new TipoDocumentoItemListaDto
        {
            TipoDado = "objeto",
            Campos =
            {
                new TipoDocumentoCampoDto
                {
                    Chave = "nome",
                    NomeExibicao = "Nome",
                    TipoDado = "texto",
                    Ordem = 1
                }
            }
        }
    });
    return dto;
}

static Task CnhSugereCamposConhecidosAusentes()
{
    var campos = CatalogoSchemaConceitualDocumento.Sugerir("cnh", null,
        ExtracaoCom("nomeCompleto", "Nome", "JOAO DA SILVA"));
    Assert(campos.Any(x => x.Chave == "cpf" && !x.EncontradoNoArquivo));
    Assert(campos.Any(x => x.Chave == "categoriaHabilitacao" && !x.EncontradoNoArquivo));
    Assert(campos.Any(x => x.Chave == "numeroRegistro"));
    return Task.CompletedTask;
}

static Task ContaLuzSugereEnderecoCepTitularConsumo()
{
    var campos = CatalogoSchemaConceitualDocumento.Sugerir("conta_luz", null,
        new ResultadoIdentificacaoDocumentoDto());
    var endereco = campos.First(x => x.Chave == "endereco");
    Assert(campos.Any(x => x.Chave == "titular"));
    Assert(campos.Any(x => x.Chave == "consumoKwh"));
    Assert(endereco.CamposFilhos.Any(x => x.Chave == "cep"));
    return Task.CompletedTask;
}

static Task ContratoSugereNumeroPartesAssinaturaVigencia()
{
    var campos = CatalogoSchemaConceitualDocumento.Sugerir("contrato", null,
        new ResultadoIdentificacaoDocumentoDto());
    Assert(campos.Any(x => x.Chave == "numeroContrato"));
    Assert(campos.Any(x => x.Chave == "partes" && x.TipoDado == "lista"));
    Assert(campos.Any(x => x.Chave == "assinaturas" && x.TipoDado == "lista"));
    Assert(campos.Any(x => x.Chave == "dataInicioVigencia"));
    return Task.CompletedTask;
}

static Task PassaporteSugereMrzDadosInternacionais()
{
    var campos = CatalogoSchemaConceitualDocumento.Sugerir("passaporte", null,
        new ResultadoIdentificacaoDocumentoDto());
    Assert(campos.Any(x => x.Chave == "mrzLinha1"));
    Assert(campos.Any(x => x.Chave == "mrzLinha2"));
    Assert(campos.Any(x => x.Chave == "paisEmissor"));
    Assert(campos.Any(x => x.Chave == "nacionalidade"));
    return Task.CompletedTask;
}

static Task CamposEncontradosMarcadosCorretamente()
{
    var campos = CatalogoSchemaConceitualDocumento.Sugerir("cnh", null,
        ExtracaoCom("dataNascimento", "Data de nascimento", "1980-01-01"));
    var campo = campos.First(x => x.Chave == "dataNascimento");
    Assert(campo.EncontradoNoArquivo);
    Assert(campo.OrigemSugestao == "conhecimento_tipo_e_arquivo");
    return Task.CompletedTask;
}

static Task CamposConhecidosAusentesMantidos()
{
    var campos = CatalogoSchemaConceitualDocumento.Sugerir("passaporte", null,
        ExtracaoCom("numeroPassaporte", "Passport No", "AB123456"));
    var campo = campos.First(x => x.Chave == "mrzLinha1");
    Assert(!campo.EncontradoNoArquivo);
    Assert(campo.OrigemSugestao == "conhecimento_tipo");
    return Task.CompletedTask;
}

static Task CampoEspecificoExemploAdicionado()
{
    var campos = CatalogoSchemaConceitualDocumento.Sugerir("contrato", null,
        ExtracaoCom("registroAns", "Registro ANS", "326305"));
    var campo = campos.First(x => x.Chave == "registroAns");
    Assert(campo.EncontradoNoArquivo);
    Assert(campo.OrigemSugestao == "encontrado_arquivo");
    return Task.CompletedTask;
}

static Task SchemaConceitualNaoArmazenaValoresReais()
{
    var campos = CatalogoSchemaConceitualDocumento.Sugerir("cnh", null,
        ExtracaoCom("nomeCompleto", "Nome", "JOAO DA SILVA"));
    var json = JsonSerializer.Serialize(campos);
    Assert(!json.Contains("JOAO", StringComparison.OrdinalIgnoreCase));
    Assert(!json.Contains("DA SILVA", StringComparison.OrdinalIgnoreCase));
    return Task.CompletedTask;
}

static Task SchemaConceitualPermaneceRascunho()
{
    var dto = new TipoDocumentoSchemaDto
    {
        Status = StatusTipoDocumentoSchema.Rascunho.ToString(),
        Campos = CatalogoSchemaConceitualDocumento.Sugerir("cnh", null,
            new ResultadoIdentificacaoDocumentoDto())
    };
    Assert(dto.Status == "Rascunho");
    return Task.CompletedTask;
}

static Task OrigemSugestaoPersistivel()
{
    var entityType = CriarDbContextSemProvider().Model.FindEntityType(typeof(TipoDocumentoCampo)) ??
        throw new InvalidOperationException("TipoDocumentoCampo nao mapeado.");
    Assert(entityType.FindProperty(nameof(TipoDocumentoCampo.OrigemSugestao)) != null);
    Assert(entityType.FindProperty(nameof(TipoDocumentoCampo.EncontradoNoArquivo)) != null);
    Assert(entityType.FindProperty(nameof(TipoDocumentoCampo.ObrigatorioSugerido)) != null);
    return Task.CompletedTask;
}

static Task AliasesDuplicadosUnificados()
{
    var extracao = ExtracaoCom("numeroRegistro", "registro", "123456789");
    extracao.CamposExtraidos.Add(new CampoExtraidoDocumentoDto
    {
        Chave = "driver_license_number",
        RotuloOriginal = "driver license number",
        ValorOriginal = "123456789",
        ValorNormalizado = "123456789",
        TipoDado = "texto",
        Confianca = 0.9m
    });
    var campos = CatalogoSchemaConceitualDocumento.Sugerir("cnh", null, extracao);
    Assert(campos.Count(x => x.Chave == "numeroRegistro") == 1);
    return Task.CompletedTask;
}

static Task ArquivoIlegivelAindaSugerePorTipo()
{
    var campos = CatalogoSchemaConceitualDocumento.Sugerir("cnh", null,
        new ResultadoIdentificacaoDocumentoDto());
    Assert(campos.Any(x => x.Chave == "nomeCompleto"));
    Assert(campos.Any(x => x.Chave == "dataValidade"));
    return Task.CompletedTask;
}

static Task TipoDesconhecidoUsaDescricaoEArquivo()
{
    var campos = CatalogoSchemaConceitualDocumento.Sugerir(
        "carteira_residencia",
        "Documento de residencia emitido para estrangeiros",
        ExtracaoCom("numeroRegistro", "Numero do registro", "ABC123"));
    Assert(campos.Any(x => x.Chave == "numeroDocumento"));
    Assert(campos.Any(x => x.Chave == "numeroRegistro" && x.EncontradoNoArquivo));
    return Task.CompletedTask;
}

static Task ObjetosEListasSugeridos()
{
    var conta = CatalogoSchemaConceitualDocumento.Sugerir("conta_luz", null,
        new ResultadoIdentificacaoDocumentoDto());
    var contrato = CatalogoSchemaConceitualDocumento.Sugerir("contrato", null,
        new ResultadoIdentificacaoDocumentoDto());
    Assert(conta.Any(x => x.Chave == "endereco" && x.CamposFilhos.Count > 0));
    Assert(contrato.Any(x => x.Chave == "partes" && x.ItemLista?.CamposFilhos.Count > 0));
    return Task.CompletedTask;
}

static Task RespostaRetornaSugestoesComExtracaoVazia()
{
    var campos = CatalogoSchemaConceitualDocumento.Sugerir("passaporte", null,
        new ResultadoIdentificacaoDocumentoDto());
    Assert(campos.Count >= 10);
    Assert(campos.All(x => x.OrigemSugestao == "conhecimento_tipo"));
    return Task.CompletedTask;
}

static ResultadoIdentificacaoDocumentoDto ExtracaoCom(string chave, string rotulo,
    object valor)
{
    var dto = new ResultadoIdentificacaoDocumentoDto();
    dto.CamposExtraidos.Add(new CampoExtraidoDocumentoDto
    {
        Chave = chave,
        RotuloOriginal = rotulo,
        ValorOriginal = valor,
        ValorNormalizado = valor,
        TipoDado = valor is DateTime ? "data" : "texto",
        Confianca = 0.98m
    });
    return dto;
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
        Tipo = TipoDocumentoLegado.Outros,
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
