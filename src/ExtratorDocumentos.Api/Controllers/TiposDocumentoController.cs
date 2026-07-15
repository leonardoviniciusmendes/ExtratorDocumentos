using ExtratorDocumentos.Application.Services.Processamento;
using ExtratorDocumentos.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/tipos-documento")]
public sealed class TiposDocumentoController : ControllerBase
{
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
            .Where(x => x.TipoDocumentoId == id)
            .OrderByDescending(x => x.CriadoEm)
            .ToListAsync(cancellationToken);
        return Ok(schemas);
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
            .Include(x => x.Campos.OrderBy(c => c.Ordem))
            .FirstOrDefaultAsync(x => x.TipoDocumentoId == id && x.Id == schemaId,
                cancellationToken);
        return schema == null ? NotFound() : Ok(schema);
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
