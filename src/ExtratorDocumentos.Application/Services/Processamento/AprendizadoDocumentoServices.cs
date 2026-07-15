using System.Globalization;
using System.Text;
using System.Text.Json;
using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public sealed class AssinaturaEstruturalDocumentoService
        : IAssinaturaEstruturalDocumentoService
    {
        public AssinaturaEstruturalDocumento Gerar(
            ResultadoIdentificacaoDocumentoDto resultado,
            CaracteristicasArquivo caracteristicas)
        {
            var assinatura = new AssinaturaEstruturalDocumento
            {
                Idioma = NormalizadorEstrutural.NormalizarToken(
                    resultado.Classificacao.IdiomasIdentificados.FirstOrDefault()),
                PaisEmissor = NormalizadorEstrutural.NormalizarToken(
                    resultado.Classificacao.PaisEmissor ??
                    resultado.Identificacao.CodigoPaisEmissor),
                CategoriaSugerida = NormalizadorEstrutural.NormalizarCodigo(
                    resultado.Classificacao.TipoIdentificado ??
                    resultado.CamposEspecificos.TipoDocumento),
                PossuiTabela = caracteristicas.PossuiTabelas,
                PossuiMrz = resultado.CamposExtraidos.Any(x =>
                    NormalizadorEstrutural.NormalizarCodigo(x.Chave).Contains("mrz",
                        StringComparison.OrdinalIgnoreCase)),
                PossuiFoto = caracteristicas.PossuiImagens,
                PossuiAssinatura = resultado.CamposExtraidos.Any(x =>
                    NormalizadorEstrutural.NormalizarCodigo(x.Chave).Contains("assinatura",
                        StringComparison.OrdinalIgnoreCase)),
                QuantidadePaginas = caracteristicas.QuantidadePaginas
            };

            assinatura.RotulosEncontrados = resultado.CamposExtraidos
                .SelectMany(x => new[] { x.RotuloOriginal, x.Chave })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => NormalizadorEstrutural.SanitizarRotulo(x!))
                .Where(x => x.Length > 1)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            assinatura.SecoesEncontradas = new[]
                {
                    resultado.Classificacao.NomeDocumentoOriginal,
                    resultado.Classificacao.SubtipoIdentificado,
                    resultado.CamposEspecificos.TipoDocumento
                }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => NormalizadorEstrutural.SanitizarRotulo(x!))
                .Where(x => x.Length > 1)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            return assinatura;
        }
    }

    public sealed class IdentificadorTipoDocumentoService
        : IIdentificadorTipoDocumentoService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;

        public IdentificadorTipoDocumentoService(AppDbContext db,
            IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task<ResultadoIdentificacaoTipoDocumento> IdentificarAsync(
            AssinaturaEstruturalDocumento assinatura,
            CancellationToken cancellationToken)
        {
            var tipos = await _db.TiposDocumento
                .Include(x => x.Schemas.Where(s => s.Ativo))
                .Where(x => x.Ativo && x.AssinaturaEstruturalJson != null)
                .ToListAsync(cancellationToken);
            var melhor = new ResultadoIdentificacaoTipoDocumento
            {
                CodigoTipo = assinatura.CategoriaSugerida ?? "outro",
                Similaridade = 0m,
                RequerRevisao = true
            };

            foreach (var tipo in tipos)
            {
                var persistida = JsonSerializer.Deserialize<AssinaturaEstruturalDocumento>(
                    tipo.AssinaturaEstruturalJson ?? "{}",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (persistida == null) continue;

                var score = CalcularSimilaridade(assinatura, persistida);
                if (score <= melhor.Similaridade) continue;

                var schema = tipo.Schemas.FirstOrDefault(x => x.Ativo);
                melhor = new ResultadoIdentificacaoTipoDocumento
                {
                    TipoDocumentoId = tipo.Id,
                    TipoDocumentoSchemaId = schema?.Id,
                    CodigoTipo = tipo.Codigo,
                    Similaridade = score,
                    TipoExistente = true,
                    RequerRevisao = score < LimiteUsoAutomatico()
                };
            }

            if (melhor.Similaridade < LimiteRevisao())
            {
                melhor.TipoDocumentoId = null;
                melhor.TipoDocumentoSchemaId = null;
                melhor.TipoExistente = false;
                melhor.RequerRevisao = true;
                melhor.CodigoTipo = assinatura.CategoriaSugerida ?? "outro";
            }

            return melhor;
        }

        private decimal CalcularSimilaridade(AssinaturaEstruturalDocumento atual,
            AssinaturaEstruturalDocumento candidata)
        {
            var pesos = Pesos();
            return Clamp01(
                pesos.Rotulos * Jaccard(atual.RotulosEncontrados, candidata.RotulosEncontrados) +
                pesos.Secoes * Jaccard(atual.SecoesEncontradas, candidata.SecoesEncontradas) +
                pesos.Categoria * Igual(atual.CategoriaSugerida, candidata.CategoriaSugerida) +
                pesos.IdiomaPais * ((Igual(atual.Idioma, candidata.Idioma) +
                    Igual(atual.PaisEmissor, candidata.PaisEmissor)) / 2m) +
                pesos.Elementos * Elementos(atual, candidata) +
                pesos.Paginas * Paginas(atual.QuantidadePaginas, candidata.QuantidadePaginas));
        }

        private decimal LimiteUsoAutomatico() =>
            ConfigDecimal("IdentificacaoDocumento:Similaridade:UsoAutomatico", 0.90m);

        private decimal LimiteRevisao() =>
            ConfigDecimal("IdentificacaoDocumento:Similaridade:RequerRevisao", 0.70m);

        private PesosSimilaridade Pesos() => new(
            ConfigDecimal("IdentificacaoDocumento:Similaridade:Pesos:Rotulos", 0.40m),
            ConfigDecimal("IdentificacaoDocumento:Similaridade:Pesos:Secoes", 0.20m),
            ConfigDecimal("IdentificacaoDocumento:Similaridade:Pesos:Categoria", 0.15m),
            ConfigDecimal("IdentificacaoDocumento:Similaridade:Pesos:IdiomaPais", 0.10m),
            ConfigDecimal("IdentificacaoDocumento:Similaridade:Pesos:Elementos", 0.10m),
            ConfigDecimal("IdentificacaoDocumento:Similaridade:Pesos:Paginas", 0.05m));

        private decimal ConfigDecimal(string chave, decimal padrao) =>
            decimal.TryParse(_configuration[chave], NumberStyles.Number,
                CultureInfo.InvariantCulture, out var valor)
                ? valor
                : padrao;

        private static decimal Jaccard(IEnumerable<string> a, IEnumerable<string> b)
        {
            var left = a.ToHashSet(StringComparer.Ordinal);
            var right = b.ToHashSet(StringComparer.Ordinal);
            if (left.Count == 0 && right.Count == 0) return 1m;
            if (left.Count == 0 || right.Count == 0) return 0m;
            return (decimal)left.Intersect(right).Count() / left.Union(right).Count();
        }

        private static decimal Igual(string? a, string? b) =>
            !string.IsNullOrWhiteSpace(a) &&
            string.Equals(a, b, StringComparison.OrdinalIgnoreCase) ? 1m : 0m;

        private static decimal Elementos(AssinaturaEstruturalDocumento a,
            AssinaturaEstruturalDocumento b)
        {
            var total = 4m;
            var acertos = 0m;
            if (a.PossuiTabela == b.PossuiTabela) acertos++;
            if (a.PossuiMrz == b.PossuiMrz) acertos++;
            if (a.PossuiFoto == b.PossuiFoto) acertos++;
            if (a.PossuiAssinatura == b.PossuiAssinatura) acertos++;
            return acertos / total;
        }

        private static decimal Paginas(int? a, int? b)
        {
            if (a == null || b == null) return 0m;
            return Math.Abs(a.Value - b.Value) <= 1 ? 1m : 0m;
        }

        private static decimal Clamp01(decimal value) => Math.Max(0m, Math.Min(1m, value));

        private sealed record PesosSimilaridade(decimal Rotulos, decimal Secoes,
            decimal Categoria, decimal IdiomaPais, decimal Elementos, decimal Paginas);
    }

    public sealed class GeradorSchemaDocumentoService : IGeradorSchemaDocumentoService
    {
        public Task<ResultadoGeracaoSchemaDocumento> GerarAsync(
            ResultadoIdentificacaoDocumentoDto resultado,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var campos = resultado.CamposExtraidos
                .Where(x => !string.IsNullOrWhiteSpace(x.Chave))
                .GroupBy(x => NormalizadorEstrutural.NormalizarChave(x.Chave))
                .Select((grupo, index) =>
                {
                    var primeiro = grupo.First();
                    var aliases = grupo.Select(x => x.RotuloOriginal)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => NormalizadorEstrutural.SanitizarRotulo(x!))
                        .Where(x => x.Length > 1)
                        .Distinct(StringComparer.Ordinal)
                        .ToList();
                    return new CampoSchemaDocumentoDto
                    {
                        Chave = grupo.Key,
                        NomeExibicao = primeiro.RotuloOriginal ?? primeiro.Chave,
                        TipoDado = NormalizadorEstrutural.TipoDado(primeiro.TipoDado),
                        Obrigatorio = false,
                        Aliases = aliases,
                        RegraNormalizacao = RegraNormalizacao(primeiro.TipoDado),
                        Ordem = index
                    };
                })
                .OrderBy(x => x.Ordem)
                .ToList();

            var codigo = NormalizadorEstrutural.NormalizarCodigo(
                resultado.Classificacao.TipoIdentificado ??
                resultado.CamposEspecificos.TipoDocumento ??
                "outro");
            var schema = new ResultadoGeracaoSchemaDocumento
            {
                CodigoTipo = codigo,
                NomeTipo = resultado.Classificacao.NomeDocumentoOriginal ?? codigo,
                Versao = "1.0",
                Campos = campos
            };
            schema.SchemaJson = JsonSerializer.Serialize(new
            {
                codigoTipo = schema.CodigoTipo,
                versao = schema.Versao,
                campos = schema.Campos.Select(x => new
                {
                    x.Chave,
                    x.TipoDado,
                    x.Obrigatorio,
                    aliases = x.Aliases,
                    x.RegraNormalizacao,
                    x.RegraValidacao
                })
            });
            return Task.FromResult(schema);
        }

        private static string? RegraNormalizacao(string tipoDado) =>
            NormalizadorEstrutural.TipoDado(tipoDado) switch
            {
                "decimal" => "decimal",
                "data" => "data",
                "data_hora" => "data_hora",
                _ => null
            };
    }

    public sealed class AplicadorSchemaDocumentoService
        : IAplicadorSchemaDocumentoService
    {
        public Task<ResultadoDocumentoPadronizadoDto> AplicarAsync(
            TipoDocumentoSchema schema,
            ResultadoIdentificacaoDocumentoDto extracao,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var campos = schema.Campos.OrderBy(x => x.Ordem).ToList();
            var encontrados = extracao.CamposExtraidos
                .GroupBy(x => NormalizadorEstrutural.NormalizarChave(x.Chave))
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
            var aliases = campos
                .SelectMany(c => LerAliases(c)
                    .Select(a => new { Alias = a, Campo = c.Chave }))
                .GroupBy(x => x.Alias)
                .ToDictionary(x => x.Key, x => x.First().Campo, StringComparer.Ordinal);

            var dados = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var campo in campos)
            {
                var valor = EncontrarValor(campo, encontrados, aliases);
                dados[campo.Chave] = valor ?? ValorAusente(campo.TipoDado);
            }

            var camposSchema = campos.Select(x => x.Chave)
                .ToHashSet(StringComparer.Ordinal);
            var adicionais = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var campo in extracao.CamposExtraidos)
            {
                var chave = NormalizadorEstrutural.NormalizarChave(campo.Chave);
                if (!camposSchema.Contains(chave))
                    adicionais[chave] = campo.ValorNormalizado ?? campo.ValorOriginal;
            }

            var obrigatoriosOk = campos.Where(x => x.Obrigatorio)
                .All(x => dados.TryGetValue(x.Chave, out var valor) && valor != null);
            var result = new ResultadoDocumentoPadronizadoDto
            {
                Dados = dados,
                CamposAdicionais = adicionais,
                Validacao =
                {
                    SchemaAplicado = true,
                    DadosObrigatoriosEncontrados = obrigatoriosOk,
                    RequerRevisaoHumana = !obrigatoriosOk
                }
            };
            return Task.FromResult(result);
        }

        private static object? EncontrarValor(TipoDocumentoCampo campo,
            IReadOnlyDictionary<string, CampoExtraidoDocumentoDto> encontrados,
            IReadOnlyDictionary<string, string> aliases)
        {
            if (encontrados.TryGetValue(campo.Chave, out var direto))
                return direto.ValorNormalizado ?? direto.ValorOriginal;

            foreach (var item in encontrados.Values)
            {
                var rotulo = NormalizadorEstrutural.SanitizarRotulo(
                    item.RotuloOriginal ?? item.Chave);
                if (aliases.TryGetValue(rotulo, out var chave) &&
                    chave == campo.Chave)
                    return item.ValorNormalizado ?? item.ValorOriginal;
            }

            return null;
        }

        private static object? ValorAusente(string tipoDado) =>
            NormalizadorEstrutural.TipoDado(tipoDado) == "lista"
                ? Array.Empty<object>()
                : null;

        private static IEnumerable<string> LerAliases(TipoDocumentoCampo campo)
        {
            if (string.IsNullOrWhiteSpace(campo.AliasesJson))
                return [];
            return JsonSerializer.Deserialize<List<string>>(campo.AliasesJson) ?? [];
        }
    }

    public sealed class TipoDocumentoAprendizadoService
        : ITipoDocumentoAprendizadoService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IGeradorSchemaDocumentoService _geradorSchema;

        public TipoDocumentoAprendizadoService(AppDbContext db,
            IConfiguration configuration,
            IGeradorSchemaDocumentoService geradorSchema)
        {
            _db = db;
            _configuration = configuration;
            _geradorSchema = geradorSchema;
        }

        public async Task<(TipoDocumento Tipo, TipoDocumentoSchema Schema, bool Reutilizado,
            decimal Similaridade, bool RequerRevisao)> ObterOuCriarAsync(
            ResultadoIdentificacaoTipoDocumento identificacao,
            AssinaturaEstruturalDocumento assinatura,
            ResultadoIdentificacaoDocumentoDto extracao,
            CancellationToken cancellationToken)
        {
            if (identificacao.TipoDocumentoId != null)
            {
                var tipo = await _db.TiposDocumento
                    .Include(x => x.Schemas.Where(s => s.Ativo))
                    .ThenInclude(x => x.Campos)
                    .FirstAsync(x => x.Id == identificacao.TipoDocumentoId,
                        cancellationToken);
                var schema = tipo.Schemas.First(x => x.Ativo);
                return (tipo, schema, true, identificacao.Similaridade,
                    identificacao.RequerRevisao);
            }

            if (!ConfigBool("IdentificacaoDocumento:Schema:CriacaoAutomatica", true))
                throw new InvalidOperationException("CRIACAO_SCHEMA_DESABILITADA");

            var gerado = await _geradorSchema.GerarAsync(extracao, cancellationToken);
            var codigo = NormalizadorEstrutural.NormalizarCodigo(
                string.IsNullOrWhiteSpace(identificacao.CodigoTipo)
                    ? gerado.CodigoTipo
                    : identificacao.CodigoTipo);
            var tipoNovo = new TipoDocumento
            {
                Codigo = codigo,
                Nome = string.IsNullOrWhiteSpace(gerado.NomeTipo) ? codigo : gerado.NomeTipo,
                Ativo = true,
                Confirmado = false,
                AssinaturaEstruturalJson = JsonSerializer.Serialize(assinatura)
            };
            var schemaNovo = new TipoDocumentoSchema
            {
                TipoDocumentoId = tipoNovo.Id,
                Versao = gerado.Versao,
                SchemaJson = gerado.SchemaJson,
                Ativo = true,
                GeradoAutomaticamente = true
            };
            var campos = gerado.Campos.Select(c => new TipoDocumentoCampo
            {
                TipoDocumentoSchemaId = schemaNovo.Id,
                Chave = c.Chave,
                NomeExibicao = c.NomeExibicao,
                TipoDado = c.TipoDado,
                Obrigatorio = false,
                AliasesJson = JsonSerializer.Serialize(c.Aliases),
                RegraNormalizacao = c.RegraNormalizacao,
                RegraValidacao = c.RegraValidacao,
                Ordem = c.Ordem
            }).ToList();

            await _db.TiposDocumento.AddAsync(tipoNovo, cancellationToken);
            await _db.TipoDocumentoSchemas.AddAsync(schemaNovo, cancellationToken);
            await _db.TipoDocumentoCampos.AddRangeAsync(campos, cancellationToken);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
            catch (DbUpdateException ex) when (EhViolacaoUnicidade(ex))
            {
                _db.ChangeTracker.Clear();
                var existente = await _db.TiposDocumento
                    .Include(x => x.Schemas.Where(s => s.Ativo))
                    .ThenInclude(x => x.Campos)
                    .FirstAsync(x => x.Codigo == codigo, cancellationToken);
                return (existente, existente.Schemas.First(x => x.Ativo),
                    true, 1m, false);
            }

            schemaNovo.Campos = campos;
            return (tipoNovo, schemaNovo, false, 0m, true);
        }

        public async Task RegistrarCamposAdicionaisAsync(
            TipoDocumento tipoDocumento,
            IReadOnlyDictionary<string, object?> camposAdicionais,
            CancellationToken cancellationToken)
        {
            if (camposAdicionais.Count == 0) return;
            var chaves = camposAdicionais.Keys
                .Select(NormalizadorEstrutural.NormalizarChave)
                .ToHashSet(StringComparer.Ordinal);
            var existentes = await _db.TipoDocumentoCamposSugeridos
                .Where(x => x.TipoDocumentoId == tipoDocumento.Id &&
                    chaves.Contains(x.Chave))
                .ToListAsync(cancellationToken);
            foreach (var chave in chaves)
            {
                var existente = existentes.FirstOrDefault(x => x.Chave == chave);
                if (existente == null)
                {
                    _db.TipoDocumentoCamposSugeridos.Add(new TipoDocumentoCampoSugerido
                    {
                        TipoDocumentoId = tipoDocumento.Id,
                        Chave = chave,
                        TipoDado = InferirTipo(camposAdicionais[chave]),
                        QuantidadeOcorrencias = 1
                    });
                    continue;
                }

                existente.QuantidadeOcorrencias++;
                existente.UltimaOcorrenciaEm = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<TipoDocumentoSchema> AtivarSchemaAsync(
            Guid tipoDocumentoId,
            Guid schemaId,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            var schemas = await _db.TipoDocumentoSchemas
                .Where(x => x.TipoDocumentoId == tipoDocumentoId)
                .ToListAsync(cancellationToken);
            foreach (var schema in schemas)
                schema.Ativo = schema.Id == schemaId;
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return schemas.First(x => x.Id == schemaId);
        }

        public async Task<TipoDocumentoSchema> AprovarCampoSugeridoAsync(
            Guid tipoDocumentoId,
            Guid campoId,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            var tipo = await _db.TiposDocumento
                .Include(x => x.Schemas.Where(s => s.Ativo))
                .ThenInclude(x => x.Campos)
                .FirstAsync(x => x.Id == tipoDocumentoId, cancellationToken);
            var atual = tipo.Schemas.First(x => x.Ativo);
            var sugerido = await _db.TipoDocumentoCamposSugeridos
                .FirstAsync(x => x.Id == campoId && x.TipoDocumentoId == tipoDocumentoId,
                    cancellationToken);
            atual.Ativo = false;
            sugerido.Aprovado = true;

            var nova = new TipoDocumentoSchema
            {
                TipoDocumentoId = tipoDocumentoId,
                Versao = ProximaVersao(atual.Versao),
                Ativo = true,
                GeradoAutomaticamente = false
            };
            await _db.TipoDocumentoSchemas.AddAsync(nova, cancellationToken);
            var campos = atual.Campos.OrderBy(x => x.Ordem)
                .Select(x => new TipoDocumentoCampo
                {
                    TipoDocumentoSchemaId = nova.Id,
                    Chave = x.Chave,
                    NomeExibicao = x.NomeExibicao,
                    TipoDado = x.TipoDado,
                    Obrigatorio = x.Obrigatorio,
                    AliasesJson = x.AliasesJson,
                    RegraNormalizacao = x.RegraNormalizacao,
                    RegraValidacao = x.RegraValidacao,
                    Ordem = x.Ordem
                })
                .ToList();
            campos.Add(new TipoDocumentoCampo
            {
                TipoDocumentoSchemaId = nova.Id,
                Chave = sugerido.Chave,
                NomeExibicao = sugerido.Chave,
                TipoDado = sugerido.TipoDado,
                Obrigatorio = false,
                Ordem = campos.Count
            });
            nova.SchemaJson = JsonSerializer.Serialize(new
            {
                codigoTipo = tipo.Codigo,
                versao = nova.Versao,
                campos = campos.Select(x => new
                {
                    x.Chave,
                    x.TipoDado,
                    x.Obrigatorio,
                    aliases = string.IsNullOrWhiteSpace(x.AliasesJson)
                        ? []
                        : JsonSerializer.Deserialize<List<string>>(x.AliasesJson)
                })
            });
            await _db.TipoDocumentoCampos.AddRangeAsync(campos, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            nova.Campos = campos;
            return nova;
        }

        private static string InferirTipo(object? valor) => valor switch
        {
            null => "texto",
            bool => "booleano",
            int or long => "inteiro",
            decimal or double or float => "decimal",
            JsonElement { ValueKind: JsonValueKind.Array } => "lista",
            JsonElement { ValueKind: JsonValueKind.Object } => "objeto",
            _ => "texto"
        };

        private static string ProximaVersao(string versao)
        {
            var partes = versao.Split('.');
            if (partes.Length == 2 && int.TryParse(partes[0], out var maior) &&
                int.TryParse(partes[1], out var menor))
                return $"{maior}.{menor + 1}";
            return "1.1";
        }

        private static bool EhViolacaoUnicidade(DbUpdateException ex) =>
            ex.InnerException?.Message.Contains("Duplicate",
                StringComparison.OrdinalIgnoreCase) == true ||
            ex.InnerException?.Message.Contains("UNIQUE",
                StringComparison.OrdinalIgnoreCase) == true;

        private bool ConfigBool(string chave, bool padrao) =>
            bool.TryParse(_configuration[chave], out var valor) ? valor : padrao;
    }

    internal static class NormalizadorEstrutural
    {
        private static readonly HashSet<string> TiposValidos = new(StringComparer.Ordinal)
        {
            "texto", "inteiro", "decimal", "booleano", "data", "data_hora",
            "lista", "objeto"
        };

        public static string TipoDado(string? tipo) =>
            TiposValidos.Contains(NormalizarCodigo(tipo ?? "texto"))
                ? NormalizarCodigo(tipo ?? "texto")
                : "texto";

        public static string NormalizarToken(string? valor) =>
            SanitizarBase(valor ?? string.Empty).Replace(" ", "_");

        public static string NormalizarCodigo(string? valor)
        {
            var normalizado = SanitizarBase(valor ?? "outro")
                .Replace(" ", "_");
            return string.IsNullOrWhiteSpace(normalizado) ? "outro" : normalizado;
        }

        public static string NormalizarChave(string valor)
        {
            var valorSeguro = valor ?? string.Empty;
            var sufixoNumerico = new string(valorSeguro
                .Reverse()
                .TakeWhile(char.IsDigit)
                .Reverse()
                .ToArray());
            var codigo = NormalizarCodigo(SepararCamelCase(valorSeguro));
            if (!codigo.Contains('_'))
                return string.IsNullOrWhiteSpace(sufixoNumerico) ||
                    codigo.EndsWith(sufixoNumerico, StringComparison.Ordinal)
                    ? codigo
                    : codigo + sufixoNumerico;
            var partes = codigo.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0) return codigo;
            var chave = partes[0] + string.Concat(partes.Skip(1)
                .Select(x => char.ToUpperInvariant(x[0]) + x[1..]));
            return string.IsNullOrWhiteSpace(sufixoNumerico) ||
                chave.EndsWith(sufixoNumerico, StringComparison.Ordinal)
                ? chave
                : chave + sufixoNumerico;
        }

        private static string SepararCamelCase(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return valor;
            var builder = new StringBuilder(valor.Length + 8);
            for (var i = 0; i < valor.Length; i++)
            {
                var ch = valor[i];
                if (i > 0 && char.IsUpper(ch) &&
                    (char.IsLower(valor[i - 1]) || char.IsDigit(valor[i - 1])))
                    builder.Append(' ');
                builder.Append(ch);
            }
            return builder.ToString();
        }

        public static string SanitizarRotulo(string valor)
        {
            var texto = SanitizarBase(valor);
            texto = texto.Replace(" n ", " numero ");
            texto = texto.Replace(" nr ", " numero ");
            texto = texto.Replace(" no ", " numero ");
            texto = texto.Replace(" contrato ", " numero do contrato ");
            return string.Join(' ', texto.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Length > 1 || x is "e" or "a"));
        }

        private static string SanitizarBase(string valor)
        {
            var semAcentos = RemoverAcentos(valor).ToLowerInvariant();
            var builder = new StringBuilder(semAcentos.Length);
            foreach (var ch in semAcentos)
                builder.Append(char.IsLetter(ch) || char.IsWhiteSpace(ch) ? ch : ' ');
            var texto = string.Join(' ', builder.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !EhVariavel(x)));
            return texto.Trim();
        }

        private static bool EhVariavel(string token) =>
            token.Length > 20 ||
            token.Contains('@', StringComparison.Ordinal) ||
            token.All(char.IsDigit);

        private static string RemoverAcentos(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) !=
                    UnicodeCategory.NonSpacingMark)
                    builder.Append(ch);
            }
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
