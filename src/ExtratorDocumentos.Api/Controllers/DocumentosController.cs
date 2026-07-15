using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Application.Services.Processamento;
using ExtratorDocumentos.Domain;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
        if (!Enum.IsDefined(request.TipoDocumento))
            return BadRequest(new { erro = "TipoDocumento invalido." });

        await using var stream = request.Arquivo.OpenReadStream();
        try
        {
            var resultado = await processador.ProcessarAsync(
                request.TipoDocumento,
                new ArquivoDocumento(request.Arquivo.FileName,
                    request.Arquivo.ContentType, stream),
                cancellationToken);

            if (string.Equals(resultado.Status, StatusExtracao.Erro.ToString(),
                    StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    erro = "Falha ao extrair dados do documento.",
                    alertas = resultado.Validacao.Alertas,
                    processamento = resultado.Processamento
                });


            if(request.TipoDocumento == TipoDocumentoProcessamento.Endereco)
            {
                var dadosEndereco = CriarRespostaDadosEndereco(resultado);
                return resultado.ResultadoReutilizado
                    ? Ok(dadosEndereco)
                    : StatusCode(StatusCodes.Status201Created, dadosEndereco);
            }
            var dados = CriarRespostaDados(resultado);
            return resultado.ResultadoReutilizado
                ? Ok(dados)
                : StatusCode(StatusCodes.Status201Created, dados);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    private static Dictionary<string, object?> CriarRespostaDados(
        ResultadoIdentificacaoDocumentoDto resultado)
    {
        var dados = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["tipoDocumento"] = resultado.Classificacao.TipoIdentificado,
            ["nomeDocumento"] = resultado.Classificacao.NomeDocumentoOriginal,
            ["paisEmissor"] = resultado.Classificacao.PaisEmissor,
            ["nomeCompleto"] = resultado.Titular.NomeCompleto,
            ["dataNascimento"] = resultado.Titular.DataNascimento,
            ["sexo"] = resultado.Titular.Sexo,
            ["nacionalidade"] = resultado.Titular.Nacionalidade,
            ["numeroDocumento"] = resultado.Identificacao.NumeroDocumento,
            ["numeroDocumentoNormalizado"] = resultado.Identificacao.NumeroDocumentoNormalizado,
            ["dataEmissao"] = resultado.Identificacao.DataEmissao,
            ["dataValidade"] = resultado.Identificacao.DataValidade,
            ["autoridadeEmissora"] = resultado.Identificacao.AutoridadeEmissora,
            ["codigoPaisEmissor"] = resultado.Identificacao.CodigoPaisEmissor
        };

        foreach (var (chave, valor) in resultado.CamposEspecificos.Valores)
        {
            if (!string.IsNullOrWhiteSpace(chave) && !dados.ContainsKey(chave))
                dados[chave] = NormalizarValorJson(valor);
        }

        foreach (var campo in resultado.CamposExtraidos)
        {
            if (string.IsNullOrWhiteSpace(campo.Chave) || dados.ContainsKey(campo.Chave))
                continue;
            dados[campo.Chave] = NormalizarValorJson(
                campo.ValorNormalizado ?? campo.ValorOriginal);
        }

        return dados;
    }

    private static Dictionary<string, object?> CriarRespostaDadosEndereco(
    ResultadoIdentificacaoDocumentoDto resultado)
    {
        var dados = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            // Classificação
            ["tipoDocumento"] = resultado.Classificacao.TipoIdentificado,
            ["nomeDocumento"] = resultado.Classificacao.NomeDocumentoOriginal,
            ["paisEmissor"] = resultado.Classificacao.PaisEmissor,

            // Titular
            ["nomeCompleto"] = resultado.Titular.NomeCompleto,

            // Endereço
            ["logradouro"] = resultado.Endereco.Logradouro,
            ["numero"] = resultado.Endereco.Numero,
            ["complemento"] = resultado.Endereco.Complemento,
            ["bairro"] = resultado.Endereco.Bairro,
            ["cidade"] = resultado.Endereco.Cidade,
            ["estado"] = resultado.Endereco.Estado,
            ["cep"] = resultado.Endereco.Cep,
            ["pais"] = resultado.Endereco.Pais,
            ["codigoPais"] = resultado.Endereco.CodigoPais,
            ["enderecoCompletoOriginal"] = resultado.Endereco.EnderecoCompletoOriginal
        };

        // Campos específicos do documento
        foreach (var (chave, valor) in resultado.CamposEspecificos.Valores)
        {
            if (!string.IsNullOrWhiteSpace(chave) && !dados.ContainsKey(chave))
            {
                dados[chave] = NormalizarValorJson(valor);
            }
        }

        // Demais campos extraídos pela IA
        foreach (var campo in resultado.CamposExtraidos)
        {
            if (string.IsNullOrWhiteSpace(campo.Chave) ||
                dados.ContainsKey(campo.Chave))
            {
                continue;
            }

            dados[campo.Chave] = NormalizarValorJson(
                campo.ValorNormalizado ?? campo.ValorOriginal);
        }

        return dados;
    }
    private static object? NormalizarValorJson(object? valor)
    {
        if (valor is not JsonElement json)
            return valor;

        return json.ValueKind switch
        {
            JsonValueKind.String => json.GetString(),
            JsonValueKind.Number when json.TryGetInt64(out var inteiro) => inteiro,
            JsonValueKind.Number when json.TryGetDecimal(out var dec) => dec,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => json.EnumerateArray()
                .Select(x => NormalizarValorJson(x))
                .ToArray(),
            JsonValueKind.Object => json.EnumerateObject()
                .ToDictionary(x => x.Name, x => NormalizarValorJson(x.Value),
                    StringComparer.Ordinal),
            _ => json.ToString()
        };
    }
}

public sealed class UploadDocumentoRequest
{
    public TipoDocumentoProcessamento TipoDocumento { get; set; }
    public IFormFile Arquivo { get; set; } = null!;
}
