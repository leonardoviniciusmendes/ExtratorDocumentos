using ExtratorDocumentos.Application.Services.Extracao;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/openrouter")]
public sealed class OpenRouterController : ControllerBase
{
    [HttpGet("modelos")]
    public async Task<IActionResult> GetModelos(
        [FromServices] OpenRouterModelosService service,
        CancellationToken cancellationToken,
        [FromQuery] bool apenasCompativeis = true)
    {
        var modelos = await service.ListarAsync(apenasCompativeis, cancellationToken);
        return Ok(modelos);
    }

    [HttpGet("creditos")]
    public async Task<IActionResult> GetCreditos(
        [FromServices] OpenRouterContaService service,
        CancellationToken cancellationToken)
    {
        var creditos = await service.ObterCreditosAsync(cancellationToken);
        return Ok(creditos);
    }
}
