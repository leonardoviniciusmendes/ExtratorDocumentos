using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Application.Services;
using ExtratorDocumentos.Application.Services.Extracao;
using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class DocumentosController : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentoRequest request,
        [FromServices] DocumentoService service, CancellationToken cancellationToken)
    {
        if (request.Arquivo == null || request.Arquivo.Length == 0)
            return BadRequest(new { erro = "Arquivo e obrigatorio." });

        await using var stream = request.Arquivo.OpenReadStream();
        try
        {
            var documento = await service.CriarAsync(request.Cpf, request.CpfDependente,
                request.Cnpj, request.Papel, request.TipoParentesco, request.Tipo,
                request.Observacoes, request.Arquivo.FileName,
                request.Arquivo.ContentType, stream, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = documento.Id }, new { documento.Id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("{id}/versoes")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> NovaVersao(Guid id, [FromForm] UploadVersaoRequest request,
        [FromServices] DocumentoService service, CancellationToken cancellationToken)
    {
        if (request.Arquivo == null || request.Arquivo.Length == 0)
            return BadRequest(new { erro = "Arquivo e obrigatorio." });
        await using var stream = request.Arquivo.OpenReadStream();
        var versao = await service.AdicionarVersaoAsync(id, request.Arquivo.FileName,
            request.Arquivo.ContentType, stream, request.Observacao, request.Usuario, cancellationToken);
        return versao == null ? NotFound() : Ok(new { versao.Id, versao.Versao, versao.HashSha256 });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DocumentoQuery query,
        [FromServices] DocumentoService service, CancellationToken cancellationToken) =>
        Ok(await service.ListarAsync(query, cancellationToken));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, [FromServices] DocumentoService service,
        CancellationToken cancellationToken)
    {
        var documento = await service.ObterAsync(id, cancellationToken);
        return documento == null ? NotFound() : Ok(documento);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id, [FromQuery] int? versao,
        [FromServices] DocumentoService service, CancellationToken cancellationToken)
    {
        var download = await service.DownloadAsync(id, versao, cancellationToken);
        return download == null ? NotFound() :
            File(download.Value.Conteudo, download.Value.Versao.TipoConteudo, download.Value.Versao.NomeArquivo);
    }

    [HttpGet("{id}/versoes")]
    public async Task<IActionResult> GetVersoes(Guid id, [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!await db.Documentos.AnyAsync(x => x.Id == id && !x.Excluido, cancellationToken)) return NotFound();
        var items = await db.DocumentoVersoes.AsNoTracking().Where(x => x.DocumentoId == id)
            .OrderByDescending(x => x.Versao)
            .Select(x => new DocumentoVersaoResponse(x.Id, x.Versao, x.NomeArquivo, x.TipoConteudo,
                x.TamanhoBytes, x.HashSha256, x.CriadoEm)).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id}/historico")]
    public async Task<IActionResult> GetHistorico(Guid id, [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!await db.Documentos.AnyAsync(x => x.Id == id, cancellationToken)) return NotFound();
        var items = await db.DocumentoHistoricos.AsNoTracking().Where(x => x.DocumentoId == id)
            .OrderByDescending(x => x.Data)
            .Select(x => new DocumentoHistoricoResponse(x.Id, x.Acao, x.StatusAnterior,
                x.StatusNovo, x.Observacao, x.Usuario, x.Data)).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id}/identificacao")]
    public async Task<IActionResult> GetIdentificacao(Guid id, [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var identificacao = await db.IdentificacoesExtraidas.AsNoTracking()
            .Where(x => x.DocumentoId == id)
            .Select(x => new IdentificacaoExtraidaResponse(x.Id, x.DocumentoId,
                x.DocumentoVersaoId, x.NomeCompleto, x.Cpf, x.Rg, x.OrgaoEmissor,
                x.UfEmissao, x.DataNascimento, x.Naturalidade, x.Nacionalidade,
                x.NomeMae, x.NomePai, x.NumeroCnh, x.CategoriaCnh, x.ValidadeCnh,
                x.DataPrimeiraHabilitacao, x.DataEmissao, x.LocalEmissao,
                x.NumeroRenach, x.ObservacoesCnh, x.Cnpj, x.RazaoSocial, x.NomeFantasia,
                x.EmissorDocumento,
                x.NumeroCliente, x.NumeroInstalacao, x.MesReferencia, x.DataVencimento,
                x.MatriculaCertidao, x.Livro, x.Folha, x.Termo, x.Confianca,
                x.Provedor, x.CriadoEm)).FirstOrDefaultAsync(cancellationToken);
        var enderecos = await ConsultarEnderecos(
            db, id, identificacao?.Cpf, identificacao?.Cnpj).ToListAsync(cancellationToken);
        if (identificacao == null && enderecos.Count == 0) return NotFound();
        var referencia = enderecos.FirstOrDefault();
        var vinculo = identificacao != null
            ? new VinculoExtracaoResponse(identificacao.Cpf, identificacao.Cnpj,
                identificacao.DocumentoId, identificacao.DocumentoVersaoId)
            : referencia == null ? null : new VinculoExtracaoResponse(referencia.Cpf,
                referencia.Cnpj, referencia.DocumentoId, referencia.DocumentoVersaoId);
        return Ok(new DadosExtraidosResponse(identificacao, enderecos, vinculo));
    }

    [HttpGet("{id}/enderecos")]
    public async Task<IActionResult> GetEnderecos(Guid id, [FromServices] AppDbContext db,
        CancellationToken cancellationToken)
    {
        var enderecos = await ConsultarEnderecos(db, id, null, null)
            .ToListAsync(cancellationToken);
        return enderecos.Count == 0 ? NotFound() : Ok(enderecos);
    }

    [HttpPost("{id}/extrair-identificacao")]
    public async Task<IActionResult> ExtrairIdentificacao(Guid id,
        [FromServices] DocumentoExtracaoService service, CancellationToken cancellationToken) =>
        await service.ExtrairAsync(id, cancellationToken) ? Ok() : UnprocessableEntity();

    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarDocumentoRequest request,
        [FromServices] DocumentoService service, CancellationToken cancellationToken) =>
        await service.AtualizarAsync(id, request, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(Guid id, [FromQuery] string? observacao,
        [FromQuery] string? usuario, [FromServices] DocumentoService service,
        CancellationToken cancellationToken) =>
        await service.ExcluirAsync(id, observacao, usuario, cancellationToken) ? NoContent() : NotFound();

    private static IQueryable<EnderecoExtraidoResponse> ConsultarEnderecos(
        AppDbContext db, Guid documentoId, string? cpf, string? cnpj) =>
        db.EnderecosExtraidos.AsNoTracking()
            .Where(x => x.DocumentoId == documentoId ||
                (cpf != null && x.Cpf == cpf) ||
                (cnpj != null && x.Cnpj == cnpj))
            .OrderByDescending(x => x.CriadoEm)
            .Select(x => new EnderecoExtraidoResponse(x.Id, x.DocumentoId,
                x.DocumentoVersaoId, x.Cpf, x.Cnpj, x.Cep, x.Logradouro, x.Numero,
                x.Complemento, x.Bairro, x.Cidade, x.Uf, x.FonteDocumento, x.CriadoEm));
}

public sealed class UploadDocumentoRequest
{
    public string? Cpf { get; set; }
    public string? CpfDependente { get; set; }
    public string? Cnpj { get; set; }
    public PapelDocumento Papel { get; set; } = PapelDocumento.Titular;
    public TipoParentesco TipoParentesco { get; set; } = TipoParentesco.Titular;
    public TipoDocumento Tipo { get; set; }
    public string? Observacoes { get; set; }
    public IFormFile Arquivo { get; set; } = null!;
}

public sealed class UploadVersaoRequest
{
    public string? Observacao { get; set; }
    public string? Usuario { get; set; }
    public IFormFile Arquivo { get; set; } = null!;
}
