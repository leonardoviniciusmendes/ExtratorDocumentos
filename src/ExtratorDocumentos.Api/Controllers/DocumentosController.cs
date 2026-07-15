using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Application.Services.Processamento;
using ExtratorDocumentos.Domain;
using Microsoft.AspNetCore.Mvc;

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
        CancellationToken cancellationToken)
    {
        if (request.Arquivo == null || request.Arquivo.Length == 0)
            return BadRequest(new { erro = "Arquivo e obrigatorio." });
        if (string.IsNullOrWhiteSpace(request.TipoDocumento))
            return BadRequest(new { erro = "TipoDocumento e obrigatorio." });

        await using var stream = request.Arquivo.OpenReadStream();
        try
        {
            var resultado = await processador.ProcessarAsync(
                request.TipoDocumento,
                new ArquivoDocumento(request.Arquivo.FileName,
                    request.Arquivo.ContentType, stream),
                cancellationToken);

            return resultado.ResultadoReutilizado
                ? Ok(resultado)
                : StatusCode(StatusCodes.Status201Created, resultado);
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
    }
}

public sealed class UploadDocumentoRequest
{
    public string TipoDocumento { get; set; } = string.Empty;
    public IFormFile Arquivo { get; set; } = null!;
}
