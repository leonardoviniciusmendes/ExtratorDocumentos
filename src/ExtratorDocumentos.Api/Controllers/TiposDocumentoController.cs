using System.Text.Json;
using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Application.Services.Processamento;
using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/tipos-documento")]
public sealed class TiposDocumentoController : ControllerBase
{
    [HttpPost("sugerir-schema")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> SugerirSchema(
        [FromForm] SugerirSchemaRequest request,
        [FromServices] ISugestorSchemaDocumentoService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TipoDocumento))
            return BadRequest(new { erro = "TipoDocumento e obrigatorio." });
        if (request.Arquivo == null || request.Arquivo.Length == 0)
            return BadRequest(new { erro = "Arquivo e obrigatorio." });

        await using var stream = request.Arquivo.OpenReadStream();
        var resultado = await service.SugerirAsync(
            request.TipoDocumento,
            request.DescricaoTipoDocumento,
            request.Pais,
            request.Idioma,
            new ArquivoDocumento(request.Arquivo.FileName,
                request.Arquivo.ContentType, stream),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var tipos = await db.TiposDocumento
            .AsNoTracking()
            .OrderBy(x => x.Codigo)
            .Select(x => new
            {
                x.Id,
                x.Codigo,
                x.Nome,
                x.Descricao,
                x.Ativo,
                x.Confirmado,
                x.CriadoEm,
                x.AtualizadoEm
            })
            .ToListAsync(cancellationToken);
        return Ok(tipos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(
        Guid id,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var tipo = await db.TiposDocumento
            .AsNoTracking()
            .Include(x => x.Schemas.Where(s => s.Ativo))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return tipo == null ? NotFound() : Ok(tipo);
    }

    [HttpGet("{id:guid}/schemas")]
    public async Task<IActionResult> ListarSchemas(
        Guid id,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var schemas = await db.TipoDocumentoSchemas
            .AsNoTracking()
            .Include(x => x.Campos)
            .Where(x => x.TipoDocumentoId == id)
            .OrderByDescending(x => x.CriadoEm)
            .ToListAsync(cancellationToken);
        return Ok(schemas.Select(ToSchemaDto));
    }

    [HttpGet("{tipoDocumento}/schemas")]
    public async Task<IActionResult> ListarSchemasPorCodigo(
        string tipoDocumento,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var tipo = await ResolverTipoAsync(db, tipoDocumento, cancellationToken);
        if (tipo == null) return NotFound();
        var schemas = await db.TipoDocumentoSchemas
            .AsNoTracking()
            .Include(x => x.Campos)
            .Where(x => x.TipoDocumentoId == tipo.Id)
            .OrderByDescending(x => x.CriadoEm)
            .ToListAsync(cancellationToken);
        return Ok(schemas.Select(ToSchemaDto));
    }

    [HttpGet("{id:guid}/schemas/{schemaId:guid}")]
    public async Task<IActionResult> ObterSchema(
        Guid id,
        Guid schemaId,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var schema = await db.TipoDocumentoSchemas
            .AsNoTracking()
            .Include(x => x.Campos)
            .FirstOrDefaultAsync(x => x.TipoDocumentoId == id && x.Id == schemaId,
                cancellationToken);
        return schema == null ? NotFound() : Ok(ToSchemaDto(schema));
    }

    [HttpGet("{tipoDocumento}/schemas/{schemaId:guid}")]
    public async Task<IActionResult> ObterSchemaPorCodigo(
        string tipoDocumento,
        Guid schemaId,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var tipo = await ResolverTipoAsync(db, tipoDocumento, cancellationToken);
        if (tipo == null) return NotFound();
        var schema = await db.TipoDocumentoSchemas
            .AsNoTracking()
            .Include(x => x.Campos)
            .FirstOrDefaultAsync(x => x.TipoDocumentoId == tipo.Id && x.Id == schemaId,
                cancellationToken);
        return schema == null ? NotFound() : Ok(ToSchemaDto(schema));
    }

    [HttpPut("{tipoDocumento}/schemas/{schemaId:guid}")]
    public async Task<IActionResult> AtualizarSchema(
        string tipoDocumento,
        Guid schemaId,
        [FromBody] TipoDocumentoSchemaDto request,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var tipo = await ResolverTipoAsync(db, tipoDocumento, cancellationToken);
        if (tipo == null) return NotFound();
        var schema = await db.TipoDocumentoSchemas
            .Include(x => x.Campos)
            .FirstOrDefaultAsync(x => x.TipoDocumentoId == tipo.Id && x.Id == schemaId,
                cancellationToken);
        if (schema == null) return NotFound();
        if (schema.Status != StatusTipoDocumentoSchema.Rascunho)
            return Conflict(new { codigo = "SCHEMA_PUBLICADO_IMUTAVEL" });

        request.Id = schema.Id;
        request.Status = StatusTipoDocumentoSchema.Rascunho.ToString();
        schema.SchemaJson = JsonSerializer.Serialize(request);
        schema.AtualizadoEm = DateTime.UtcNow;
        db.TipoDocumentoCampos.RemoveRange(schema.Campos);
        db.TipoDocumentoCampos.AddRange(CriarCampos(schema.Id, request.Campos, null));
        await db.SaveChangesAsync(cancellationToken);
        return Ok(request);
    }

    [HttpPost("{tipoDocumento}/schemas/{schemaId:guid}/validar")]
    public async Task<IActionResult> ValidarSchema(
        string tipoDocumento,
        Guid schemaId,
        [FromServices] AppDbContext db,
        [FromServices] IValidadorSchemaDocumentoService validador,
        CancellationToken cancellationToken)
    {
        var tipo = await ResolverTipoAsync(db, tipoDocumento, cancellationToken);
        if (tipo == null) return NotFound();
        var schema = await db.TipoDocumentoSchemas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TipoDocumentoId == tipo.Id && x.Id == schemaId,
                cancellationToken);
        if (schema == null) return NotFound();
        var dto = JsonSerializer.Deserialize<TipoDocumentoSchemaDto>(
            schema.SchemaJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        return Ok(validador.Validar(dto));
    }

    [HttpPost("{tipoDocumento}/schemas/{schemaId:guid}/publicar")]
    public async Task<IActionResult> PublicarSchema(
        string tipoDocumento,
        Guid schemaId,
        [FromServices] AppDbContext db,
        [FromServices] IPublicadorSchemaDocumentoService publicador,
        CancellationToken cancellationToken)
    {
        var tipo = await ResolverTipoAsync(db, tipoDocumento, cancellationToken);
        if (tipo == null) return NotFound();
        await publicador.PublicarAsync(tipo.Id, schemaId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{tipoDocumento}/schemas/{schemaId:guid}/nova-versao")]
    public async Task<IActionResult> NovaVersao(
        string tipoDocumento,
        Guid schemaId,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var tipo = await ResolverTipoAsync(db, tipoDocumento, cancellationToken);
        if (tipo == null) return NotFound();
        var origem = await db.TipoDocumentoSchemas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TipoDocumentoId == tipo.Id && x.Id == schemaId,
                cancellationToken);
        if (origem == null) return NotFound();
        var dto = JsonSerializer.Deserialize<TipoDocumentoSchemaDto>(
            origem.SchemaJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        dto.Id = Guid.NewGuid();
        dto.Versao = ProximaVersao(origem.Versao);
        dto.Status = StatusTipoDocumentoSchema.Rascunho.ToString();
        dto.GeradoAutomaticamente = false;
        var novo = new TipoDocumentoSchema
        {
            Id = dto.Id,
            TipoDocumentoId = tipo.Id,
            Versao = dto.Versao,
            Status = StatusTipoDocumentoSchema.Rascunho,
            Ativo = false,
            GeradoAutomaticamente = false,
            SchemaJson = JsonSerializer.Serialize(dto),
            AtualizadoEm = DateTime.UtcNow
        };
        db.TipoDocumentoSchemas.Add(novo);
        db.TipoDocumentoCampos.AddRange(CriarCampos(novo.Id, dto.Campos, null));
        await db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpPost("{id:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(
        Guid id,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var tipo = await db.TiposDocumento.FirstOrDefaultAsync(x => x.Id == id,
            cancellationToken);
        if (tipo == null) return NotFound();
        tipo.Confirmado = true;
        tipo.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(tipo);
    }

    [HttpPost("{id:guid}/desativar")]
    public async Task<IActionResult> Desativar(
        Guid id,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var tipo = await db.TiposDocumento.FirstOrDefaultAsync(x => x.Id == id,
            cancellationToken);
        if (tipo == null) return NotFound();
        tipo.Ativo = false;
        tipo.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(tipo);
    }

    [HttpPost("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(
        Guid id,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var tipo = await db.TiposDocumento.FirstOrDefaultAsync(x => x.Id == id,
            cancellationToken);
        if (tipo == null) return NotFound();
        tipo.Ativo = true;
        tipo.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(tipo);
    }

    [HttpPost("{id:guid}/schemas/{schemaId:guid}/ativar")]
    public async Task<IActionResult> AtivarSchema(
        Guid id,
        Guid schemaId,
        [FromServices] ITipoDocumentoAprendizadoService service,
        CancellationToken cancellationToken)
    {
        var schema = await service.AtivarSchemaAsync(id, schemaId, cancellationToken);
        return Ok(schema);
    }

    [HttpPut("{id:guid}/schemas/{schemaId:guid}/campos")]
    public async Task<IActionResult> AtualizarCampos(
        Guid id,
        Guid schemaId,
        [FromBody] AtualizarCamposSchemaRequest request,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var schema = await db.TipoDocumentoSchemas
            .Include(x => x.Campos)
            .FirstOrDefaultAsync(x => x.TipoDocumentoId == id && x.Id == schemaId,
                cancellationToken);
        if (schema == null) return NotFound();

        db.TipoDocumentoCampos.RemoveRange(schema.Campos);
        var ordem = 0;
        foreach (var campo in request.Campos)
        {
            db.TipoDocumentoCampos.Add(new()
            {
                TipoDocumentoSchemaId = schema.Id,
                Chave = campo.Chave,
                NomeExibicao = campo.NomeExibicao,
                TipoDado = campo.TipoDado,
                Obrigatorio = campo.Obrigatorio,
                AliasesJson = System.Text.Json.JsonSerializer.Serialize(campo.Aliases),
                RegraNormalizacao = campo.RegraNormalizacao,
                RegraValidacao = campo.RegraValidacao,
                Ordem = ordem++
            });
        }

        schema.SchemaJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            schema.Versao,
            campos = request.Campos
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(schema);
    }

    [HttpGet("{id:guid}/campos-sugeridos")]
    public async Task<IActionResult> ListarCamposSugeridos(
        Guid id,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var campos = await db.TipoDocumentoCamposSugeridos
            .AsNoTracking()
            .Where(x => x.TipoDocumentoId == id)
            .OrderByDescending(x => x.QuantidadeOcorrencias)
            .ToListAsync(cancellationToken);
        return Ok(campos);
    }

    [HttpPost("{id:guid}/campos-sugeridos/{campoId:guid}/aprovar")]
    public async Task<IActionResult> AprovarCampoSugerido(
        Guid id,
        Guid campoId,
        [FromServices] ITipoDocumentoAprendizadoService service,
        CancellationToken cancellationToken)
    {
        var schema = await service.AprovarCampoSugeridoAsync(id, campoId,
            cancellationToken);
        return Ok(schema);
    }

    [HttpPost("{id:guid}/campos-sugeridos/{campoId:guid}/rejeitar")]
    public async Task<IActionResult> RejeitarCampoSugerido(
        Guid id,
        Guid campoId,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var campo = await db.TipoDocumentoCamposSugeridos
            .FirstOrDefaultAsync(x => x.TipoDocumentoId == id && x.Id == campoId,
                cancellationToken);
        if (campo == null) return NotFound();
        db.TipoDocumentoCamposSugeridos.Remove(campo);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static Task<TipoDocumento?> ResolverTipoAsync(AppDbContext db,
        string tipoDocumento, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(tipoDocumento, out var id))
            return db.TiposDocumento.FirstOrDefaultAsync(x => x.Id == id,
                cancellationToken);

        var codigo = NormalizarCodigo(tipoDocumento);
        return db.TiposDocumento.FirstOrDefaultAsync(x => x.Codigo == codigo,
            cancellationToken);
    }

    private static List<TipoDocumentoCampo> CriarCampos(Guid schemaId,
        IEnumerable<CampoSchemaSugeridoDto> campos, Guid? paiId)
    {
        var result = new List<TipoDocumentoCampo>();
        foreach (var campo in campos.OrderBy(x => x.Ordem))
        {
            var entidade = new TipoDocumentoCampo
            {
                TipoDocumentoSchemaId = schemaId,
                CampoPaiId = paiId,
                Chave = NormalizarChave(campo.Chave),
                NomeExibicao = campo.NomeExibicao,
                TipoDado = campo.TipoDado,
                ItemTipoDado = campo.ItemLista?.TipoDado,
                Obrigatorio = campo.ObrigatorioSugerido,
                ObrigatorioSugerido = campo.ObrigatorioSugerido,
                OrigemSugestao = campo.OrigemSugestao,
                EncontradoNoArquivo = campo.EncontradoNoArquivo,
                Confianca = campo.Confianca,
                AliasesJson = JsonSerializer.Serialize(campo.Aliases),
                RegraNormalizacao = campo.RegraNormalizacao,
                RegraValidacao = campo.RegraValidacao,
                Descricao = campo.Descricao,
                Ordem = campo.Ordem
            };
            result.Add(entidade);
            result.AddRange(CriarCampos(schemaId, campo.CamposFilhos, entidade.Id));
            if (campo.ItemLista?.CamposFilhos.Count > 0)
                result.AddRange(CriarCampos(schemaId, campo.ItemLista.CamposFilhos, entidade.Id));
        }

        return result;
    }

    private static TipoDocumentoSchemaDto ToSchemaDto(TipoDocumentoSchema schema)
    {
        var dto = DesserializarSchema(schema.SchemaJson);
        dto.Id = schema.Id;
        dto.Versao = schema.Versao;
        dto.Status = schema.Status.ToString();
        dto.GeradoAutomaticamente = schema.GeradoAutomaticamente;

        if (dto.Campos.Count == 0 && schema.Campos.Count > 0)
            dto.Campos = CamposDto(schema.Campos, null);

        if (dto.JsonExemplo.Count == 0 && dto.Campos.Count > 0)
            dto.JsonExemplo = CriarJsonExemplo(dto.Campos);

        return dto;
    }

    private static TipoDocumentoSchemaDto DesserializarSchema(string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
            return new TipoDocumentoSchemaDto();

        return JsonSerializer.Deserialize<TipoDocumentoSchemaDto>(
            schemaJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ??
            new TipoDocumentoSchemaDto();
    }

    private static List<CampoSchemaSugeridoDto> CamposDto(
        IEnumerable<TipoDocumentoCampo> campos, Guid? paiId)
    {
        return campos
            .Where(x => x.CampoPaiId == paiId)
            .OrderBy(x => x.Ordem)
            .Select(campo =>
            {
                var dto = new CampoSchemaSugeridoDto
                {
                    Chave = campo.Chave,
                    NomeExibicao = campo.NomeExibicao,
                    Descricao = campo.Descricao,
                    TipoDado = campo.TipoDado,
                    ObrigatorioSugerido = campo.ObrigatorioSugerido || campo.Obrigatorio,
                    OrigemSugestao = string.IsNullOrWhiteSpace(campo.OrigemSugestao)
                        ? "conhecimento_tipo"
                        : campo.OrigemSugestao,
                    EncontradoNoArquivo = campo.EncontradoNoArquivo,
                    Confianca = campo.Confianca,
                    Aliases = LerAliases(campo.AliasesJson),
                    RegraNormalizacao = campo.RegraNormalizacao,
                    RegraValidacao = campo.RegraValidacao,
                    Ordem = campo.Ordem,
                    CamposFilhos = CamposDto(campos, campo.Id)
                };

                if (campo.TipoDado == "lista")
                {
                    dto.ItemLista = new CampoSchemaSugeridoDto
                    {
                        Chave = "item",
                        NomeExibicao = "Item",
                        TipoDado = campo.ItemTipoDado ?? "objeto",
                        CamposFilhos = CamposDto(campos, campo.Id)
                    };
                    dto.CamposFilhos = new List<CampoSchemaSugeridoDto>();
                }

                return dto;
            })
            .ToList();
    }

    private static List<string> LerAliases(string? aliasesJson)
    {
        if (string.IsNullOrWhiteSpace(aliasesJson))
            return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(aliasesJson) ??
                new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    private static Dictionary<string, object?> CriarJsonExemplo(
        IEnumerable<CampoSchemaSugeridoDto> campos)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var campo in campos.OrderBy(x => x.Ordem))
        {
            result[campo.Chave] = campo.TipoDado switch
            {
                "objeto" => CriarJsonExemplo(campo.CamposFilhos),
                "lista" => Array.Empty<object>(),
                _ => null
            };
        }

        return result;
    }

    private static string ProximaVersao(string versao)
    {
        var partes = versao.Split('.');
        if (partes.Length == 2 && int.TryParse(partes[0], out var maior) &&
            int.TryParse(partes[1], out var menor))
            return $"{maior}.{menor + 1}";
        return "1.1";
    }

    private static string NormalizarCodigo(string valor) =>
        string.Join('_', valor.Trim().ToLowerInvariant()
            .Replace("-", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string NormalizarChave(string valor)
    {
        var codigo = NormalizarCodigo(valor);
        if (!codigo.Contains('_')) return codigo;
        var partes = codigo.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return partes[0] + string.Concat(partes.Skip(1)
            .Select(x => char.ToUpperInvariant(x[0]) + x[1..]));
    }
}

public sealed class AtualizarCamposSchemaRequest
{
    public List<AtualizarCampoSchemaRequest> Campos { get; set; } = new();
}

public sealed class AtualizarCampoSchemaRequest
{
    public string Chave { get; set; } = string.Empty;
    public string NomeExibicao { get; set; } = string.Empty;
    public string TipoDado { get; set; } = "texto";
    public bool Obrigatorio { get; set; }
    public List<string> Aliases { get; set; } = new();
    public string? RegraNormalizacao { get; set; }
    public string? RegraValidacao { get; set; }
}

public sealed class SugerirSchemaRequest
{
    public string TipoDocumento { get; set; } = string.Empty;
    public string? DescricaoTipoDocumento { get; set; }
    public string? Pais { get; set; }
    public string? Idioma { get; set; }
    public IFormFile Arquivo { get; set; } = null!;
}
