using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Application.Services.Processamento;
using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentosController : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentoRequest request,
        [FromServices] IProcessadorDocumentoIdentificacaoService processador,
        [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (request.Arquivo == null || request.Arquivo.Length == 0)
            return BadRequest(new { erro = "Arquivo e obrigatorio." });
        if (request.TipoDocumento == Guid.Empty)
            return BadRequest(new { erro = "TipoDocumento e obrigatorio." });

        var tipoDocumento = await ResolverTipoDocumentoAsync(
            db, request.TipoDocumento, cancellationToken);
        if (tipoDocumento == null)
            return UnprocessableEntity(new
            {
                codigo = "TIPO_DOCUMENTO_NAO_CADASTRADO",
                mensagem = "TipoDocumento nao cadastrado ou inativo."
            });
        if (!tipoDocumento.Schemas.Any(s =>
                s.Ativo && s.Status == StatusTipoDocumentoSchema.Ativo))
            return UnprocessableEntity(new
            {
                codigo = "SCHEMA_ATIVO_NAO_ENCONTRADO",
                mensagem = $"Nao existe um schema ativo para o tipo {tipoDocumento.Codigo}."
            });

        await using var stream = request.Arquivo.OpenReadStream();
        try
        {
            var resultado = await processador.ProcessarAsync(
                tipoDocumento.Codigo,
                new ArquivoDocumento(request.Arquivo.FileName,
                    request.Arquivo.ContentType, stream),
                cancellationToken);

            return resultado.ResultadoReutilizado
                ? Ok(resultado.Dados)
                : StatusCode(StatusCodes.Status201Created, resultado.Dados);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
        catch (InvalidOperationException ex) when (
            ex.Message.StartsWith("SCHEMA_ATIVO_NAO_ENCONTRADO:",
                StringComparison.OrdinalIgnoreCase))
        {
            var mensagem = ex.Message.Split(':', 2)[1].Trim();
            return UnprocessableEntity(new
            {
                codigo = "SCHEMA_ATIVO_NAO_ENCONTRADO",
                mensagem
            });
        }
        catch (InvalidOperationException ex) when (
            ex.Message.StartsWith("PROCESSAMENTO_EM_ANDAMENTO:",
                StringComparison.OrdinalIgnoreCase))
        {
            var mensagem = ex.Message.Split(':', 2)[1].Trim();
            return StatusCode(StatusCodes.Status409Conflict, new
            {
                codigo = "PROCESSAMENTO_EM_ANDAMENTO",
                mensagem
            });
        }
    }


    [HttpGet("tipos")]
    public async Task<IActionResult> GetTipos(
       [FromServices] AppDbContext db,
       CancellationToken cancellationToken)
    {
        var tipos = await db.TiposDocumento
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new
            {
                x.Id,
                x.Codigo,
                x.Nome,
                x.Descricao,
                TemSchemaAtivo = x.Schemas.Any(s =>
                    s.Ativo && s.Status == StatusTipoDocumentoSchema.Ativo),
                SchemaAtivo = x.Schemas
                    .Where(s => s.Ativo && s.Status == StatusTipoDocumentoSchema.Ativo)
                    .OrderByDescending(s => s.PublicadoEm ?? s.AtualizadoEm)
                    .Select(s => new
                    {
                        s.Id,
                        s.Versao
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        return Ok(tipos);
    }

    private static Task<TipoDocumento?> ResolverTipoDocumentoAsync(
        AppDbContext db,
        Guid tipoDocumento,
        CancellationToken cancellationToken)
    {
        return db.TiposDocumento
            .Include(x => x.Schemas.Where(s =>
                s.Ativo && s.Status == StatusTipoDocumentoSchema.Ativo))
            .FirstOrDefaultAsync(x =>
                x.Ativo &&
                x.Id == tipoDocumento,
                cancellationToken);
    }
}

public sealed class UploadDocumentoRequest
{
    public Guid TipoDocumento { get; set; }
    public IFormFile Arquivo { get; set; } = null!;
}
