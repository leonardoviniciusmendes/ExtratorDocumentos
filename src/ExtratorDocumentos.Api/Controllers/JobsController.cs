using ExtratorDocumentos.Application.Jobs;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    [HttpPost("processar-documentos-pendentes")]
    public async Task<IActionResult> Processar([FromServices] ProcessarDocumentosPendentesJob job,
        CancellationToken cancellationToken) => Ok(await job.ExecutarAsync(cancellationToken));
}
