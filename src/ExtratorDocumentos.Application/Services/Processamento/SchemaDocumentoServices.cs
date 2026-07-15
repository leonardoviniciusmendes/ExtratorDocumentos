using System.Security.Cryptography;
using System.Text.Json;
using ExtratorDocumentos.Application.Dtos.Documentos;
using ExtratorDocumentos.Domain;
using ExtratorDocumentos.Infrastructure.Data;
using ExtratorDocumentos.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public sealed class SugestorSchemaDocumentoService : ISugestorSchemaDocumentoService
    {
        private readonly AppDbContext _db;
        private readonly LocalStorageService _storage;
        private readonly IArquivoAnaliseService _analiseService;
        private readonly IOpenRouterModelCandidateSelector _selector;
        private readonly IOpenRouterPipelineClient _openRouter;
        private readonly IGeradorSchemaDocumentoService _geradorSchema;
        private readonly IConfiguration _configuration;

        public SugestorSchemaDocumentoService(AppDbContext db,
            LocalStorageService storage,
            IArquivoAnaliseService analiseService,
            IOpenRouterModelCandidateSelector selector,
            IOpenRouterPipelineClient openRouter,
            IGeradorSchemaDocumentoService geradorSchema,
            IConfiguration configuration)
        {
            _db = db;
            _storage = storage;
            _analiseService = analiseService;
            _selector = selector;
            _openRouter = openRouter;
            _geradorSchema = geradorSchema;
            _configuration = configuration;
        }

        public async Task<ResultadoSugestaoSchemaDto> SugerirAsync(
            string tipoDocumento,
            string? descricaoTipoDocumento,
            string? pais,
            string? idioma,
            ArquivoDocumento arquivo,
            CancellationToken cancellationToken)
        {
            var codigoTipo = NormalizadorEstrutural.NormalizarCodigo(tipoDocumento);
            if (string.IsNullOrWhiteSpace(codigoTipo) || codigoTipo == "outro")
                throw new ArgumentException("TipoDocumento invalido.");

            var bytes = await LerArquivoAsync(arquivo.Conteudo, cancellationToken);
            var hash = CalcularHash(bytes);
            var documento = await ObterOuCriarDocumentoReferenciaAsync(
                arquivo, bytes, hash, cancellationToken);
            var caracteristicas = await _analiseService.AnalisarAsync(
                arquivo.NomeOriginal, arquivo.MimeType, bytes, cancellationToken);
            var modelos = await _selector.SelecionarCandidatosParaExtracaoAsync(
                new DocumentoIdentificado(TipoDocumentoLegado.Outros, 0, "{}"),
                caracteristicas, cancellationToken);
            var contextoPrompt = CriarContextoPrompt(codigoTipo, descricaoTipoDocumento,
                pais, idioma);
            var chamada = await ExtrairComFallbackAsync(
                documento.Id, modelos, contextoPrompt, caracteristicas, bytes,
                cancellationToken);
            var extracao = chamada.Dados ?? new ResultadoIdentificacaoDocumentoDto();
            extracao.Classificacao.TipoIdentificado = codigoTipo;
            extracao.CamposEspecificos.TipoDocumento = codigoTipo;

            var camposDto = CatalogoSchemaConceitualDocumento.Sugerir(
                codigoTipo, descricaoTipoDocumento, extracao);
            var nomeTipo = CatalogoSchemaConceitualDocumento.ObterNomeTipo(
                codigoTipo, descricaoTipoDocumento);
            var tipo = await ObterOuCriarTipoAsync(codigoTipo, nomeTipo,
                cancellationToken);
            var versao = await ProximaVersaoRascunhoAsync(tipo.Id, cancellationToken);
            var schemaDto = new TipoDocumentoSchemaDto
            {
                Id = Guid.NewGuid(),
                Versao = versao,
                Status = StatusTipoDocumentoSchema.Rascunho.ToString(),
                GeradoAutomaticamente = true,
                Campos = camposDto,
                JsonExemplo = CriarJsonExemplo(camposDto)
            };
            var schema = new TipoDocumentoSchema
            {
                Id = schemaDto.Id,
                TipoDocumentoId = tipo.Id,
                DocumentoReferenciaId = documento.Id,
                Versao = versao,
                Status = StatusTipoDocumentoSchema.Rascunho,
                Ativo = false,
                GeradoAutomaticamente = true,
                SchemaJson = JsonSerializer.Serialize(schemaDto),
                AtualizadoEm = DateTime.UtcNow
            };
            await _db.TipoDocumentoSchemas.AddAsync(schema, cancellationToken);
            await _db.TipoDocumentoCampos.AddRangeAsync(
                CriarCamposEntidade(schema.Id, camposDto, null), cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return new ResultadoSugestaoSchemaDto
            {
                TipoDocumento =
                {
                    Id = tipo.Id,
                    Codigo = tipo.Codigo,
                    Nome = tipo.Nome
                },
                DocumentoReferencia =
                {
                    Id = documento.Id,
                    NomeArquivo = documento.NomeOriginal,
                    HashSha256 = documento.HashSha256
                },
                Schema = schemaDto,
                Analise =
                {
                    ModeloUtilizado = chamada.ModeloId,
                    Confianca = extracao.Classificacao.Confianca,
                    CamposSugeridos = camposDto.Count,
                    Alertas = chamada.Sucesso
                        ? new List<string>()
                        : new List<string>
                        {
                            chamada.Erro ?? "Nao foi possivel analisar o arquivo de exemplo no OpenRouter; schema sugerido apenas pelo tipo documental."
                        }
                },
                AnaliseArquivo =
                {
                    CamposEncontrados = camposDto
                        .Where(x => x.EncontradoNoArquivo)
                        .Select(x => x.Chave)
                        .ToList(),
                    CamposNaoEncontradosMasSugeridos = camposDto
                        .Where(x => !x.EncontradoNoArquivo)
                        .Select(x => x.Chave)
                        .ToList(),
                    QuantidadeCamposEncontrados = camposDto.Count(x => x.EncontradoNoArquivo),
                    QuantidadeTotalCamposSugeridos = camposDto.Count
                }
            };
        }

        private async Task<ResultadoChamadaModelo<ResultadoIdentificacaoDocumentoDto>> ExtrairComFallbackAsync(
            Guid documentoId,
            IReadOnlyList<ModeloSelecionado> modelos,
            string tipoDocumento,
            CaracteristicasArquivo caracteristicas,
            byte[] bytes,
            CancellationToken cancellationToken)
        {
            ResultadoChamadaModelo<ResultadoIdentificacaoDocumentoDto>? ultimo = null;
            var candidatos = modelos.Take(QuantidadeMaximaTentativas()).ToList();
            if (candidatos.Count == 0)
                return new ResultadoChamadaModelo<ResultadoIdentificacaoDocumentoDto>(
                    null, "nenhum_modelo", null, null, null, 0, false,
                    "MODELOS_INDISPONIVEIS", null);

            foreach (var modelo in candidatos)
            {
                ultimo = await _openRouter.ExtrairIdentificacaoUniversalAsync(
                    modelo, tipoDocumento, caracteristicas, bytes, null,
                    cancellationToken);
                _db.UsosOpenRouter.Add(new UsoOpenRouter
                {
                    DocumentoId = documentoId,
                    ModeloId = ultimo.ModeloId,
                    Objetivo = "sugestao_schema",
                    TokensEntrada = ultimo.TokensEntrada,
                    TokensSaida = ultimo.TokensSaida,
                    Custo = ultimo.Custo,
                    DuracaoMs = ultimo.DuracaoMs,
                    Sucesso = ultimo.Sucesso,
                    Erro = ultimo.Erro,
                    RequestId = ultimo.RequestId
                });
                await _db.SaveChangesAsync(cancellationToken);
                if (ultimo.Sucesso && ultimo.Dados != null)
                    return ultimo;
            }

            return ultimo ?? new ResultadoChamadaModelo<ResultadoIdentificacaoDocumentoDto>(
                null, "nenhum_modelo", null, null, null, 0, false,
                "MODELOS_INDISPONIVEIS", null);
        }

        private async Task<Documento> ObterOuCriarDocumentoReferenciaAsync(
            ArquivoDocumento arquivo, byte[] bytes, string hash,
            CancellationToken cancellationToken)
        {
            var existente = await _db.Documentos
                .FirstOrDefaultAsync(x => x.HashSha256 == hash && !x.Excluido,
                    cancellationToken);
            if (existente != null) return existente;

            var caracteristicas = await _analiseService.AnalisarAsync(
                arquivo.NomeOriginal, arquivo.MimeType, bytes, cancellationToken);
            var documento = new Documento
            {
                NomeOriginal = Path.GetFileName(arquivo.NomeOriginal),
                HashSha256 = hash,
                Extensao = caracteristicas.Extensao,
                MimeType = caracteristicas.MimeType,
                TamanhoBytes = bytes.LongLength,
                Tipo = TipoDocumentoLegado.Outros,
                Status = StatusDocumento.Pendente,
                VersaoSchema = "schema-referencia",
                VersaoExtrator = _configuration["DocumentProcessing:VersaoExtrator"] ?? "1.0.0"
            };
            var chave = StorageKeyBuilder.CriarChaveOriginal(documento, 1,
                arquivo.NomeOriginal);
            await using var stream = new MemoryStream(bytes);
            await _storage.SalvarAsync(chave, stream, cancellationToken);
            await _db.Documentos.AddAsync(documento, cancellationToken);
            await _db.DocumentoVersoes.AddAsync(new DocumentoVersao
            {
                DocumentoId = documento.Id,
                Versao = 1,
                NomeArquivo = Path.GetFileName(arquivo.NomeOriginal),
                TipoConteudo = documento.MimeType,
                TamanhoBytes = bytes.LongLength,
                HashSha256 = hash,
                ChaveStorage = chave
            }, cancellationToken);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return documento;
            }
            catch (DbUpdateException ex) when (EhViolacaoUnicidade(ex))
            {
                _db.Entry(documento).State = EntityState.Detached;
                return await _db.Documentos.FirstAsync(
                    x => x.HashSha256 == hash && !x.Excluido, cancellationToken);
            }
        }

        private async Task<TipoDocumento> ObterOuCriarTipoAsync(string codigo,
            string nome, CancellationToken cancellationToken)
        {
            var existente = await _db.TiposDocumento
                .FirstOrDefaultAsync(x => x.Codigo == codigo, cancellationToken);
            if (existente != null) return existente;

            var tipo = new TipoDocumento
            {
                Codigo = codigo,
                Nome = string.IsNullOrWhiteSpace(nome) ? codigo : nome,
                Ativo = true,
                Confirmado = false
            };
            await _db.TiposDocumento.AddAsync(tipo, cancellationToken);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return tipo;
            }
            catch (DbUpdateException ex) when (EhViolacaoUnicidade(ex))
            {
                _db.Entry(tipo).State = EntityState.Detached;
                return await _db.TiposDocumento.FirstAsync(x => x.Codigo == codigo,
                    cancellationToken);
            }
        }

        private async Task<string> ProximaVersaoRascunhoAsync(Guid tipoDocumentoId,
            CancellationToken cancellationToken)
        {
            var versoes = await _db.TipoDocumentoSchemas
                .Where(x => x.TipoDocumentoId == tipoDocumentoId)
                .Select(x => x.Versao)
                .ToListAsync(cancellationToken);
            if (versoes.Count == 0) return "1.0";
            var maior = versoes.Select(NumeroVersao).DefaultIfEmpty(10).Max();
            return $"{maior / 10}.{maior % 10 + 1}";
        }

        internal static List<TipoDocumentoCampo> CriarCamposEntidade(
            Guid schemaId, IEnumerable<CampoSchemaSugeridoDto> campos, Guid? paiId)
        {
            var result = new List<TipoDocumentoCampo>();
            foreach (var campo in campos.OrderBy(x => x.Ordem))
            {
                var entidade = new TipoDocumentoCampo
                {
                    TipoDocumentoSchemaId = schemaId,
                    CampoPaiId = paiId,
                    Chave = NormalizadorEstrutural.NormalizarChave(campo.Chave),
                    NomeExibicao = campo.NomeExibicao,
                    TipoDado = NormalizadorEstrutural.TipoDado(campo.TipoDado),
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
                result.AddRange(CriarCamposEntidade(schemaId, campo.CamposFilhos, entidade.Id));
                if (campo.ItemLista?.CamposFilhos.Count > 0)
                    result.AddRange(CriarCamposEntidade(schemaId, campo.ItemLista.CamposFilhos,
                        entidade.Id));
            }

            return result;
        }

        internal static Dictionary<string, object?> CriarJsonExemplo(
            IEnumerable<CampoSchemaSugeridoDto> campos)
        {
            var result = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var campo in campos.OrderBy(x => x.Ordem))
            {
                var chave = NormalizadorEstrutural.NormalizarChave(campo.Chave);
                result[chave] = NormalizadorEstrutural.TipoDado(campo.TipoDado) switch
                {
                    "objeto" => CriarJsonExemplo(campo.CamposFilhos),
                    "lista" => Array.Empty<object>(),
                    _ => null
                };
            }

            return result;
        }

        private int QuantidadeMaximaTentativas()
        {
            if (!int.TryParse(_configuration["OpenRouter:Selecao:QuantidadeMaximaFallbacks"],
                    out var fallbacks))
                fallbacks = 2;
            return Math.Max(1, fallbacks + 1);
        }

        private static async Task<byte[]> LerArquivoAsync(Stream stream,
            CancellationToken cancellationToken)
        {
            await using var memoria = new MemoryStream();
            await stream.CopyToAsync(memoria, cancellationToken);
            if (memoria.Length == 0)
                throw new ArgumentException("Arquivo vazio.");
            return memoria.ToArray();
        }

        private static string CalcularHash(byte[] bytes) =>
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        private static int NumeroVersao(string versao)
        {
            var partes = versao.Split('.');
            if (partes.Length == 2 && int.TryParse(partes[0], out var maior) &&
                int.TryParse(partes[1], out var menor))
                return maior * 10 + menor;
            return 10;
        }

        private static bool EhViolacaoUnicidade(DbUpdateException ex) =>
            ex.InnerException?.Message.Contains("Duplicate",
                StringComparison.OrdinalIgnoreCase) == true ||
            ex.InnerException?.Message.Contains("UNIQUE",
                StringComparison.OrdinalIgnoreCase) == true;

        private static string CriarContextoPrompt(string tipoDocumento,
            string? descricaoTipoDocumento, string? pais, string? idioma)
        {
            return string.Join(Environment.NewLine, new[]
            {
                $"Tipo documental: {tipoDocumento}",
                string.IsNullOrWhiteSpace(descricaoTipoDocumento)
                    ? null
                    : $"Descricao opcional: {descricaoTipoDocumento}",
                string.IsNullOrWhiteSpace(pais) ? null : $"Pais opcional: {pais}",
                string.IsNullOrWhiteSpace(idioma) ? null : $"Idioma opcional: {idioma}",
                "O arquivo anexado e somente um exemplo desse tipo. Nao limite o schema aos campos encontrados no exemplo."
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }

    public static class CatalogoSchemaConceitualDocumento
    {
        private const string ConhecimentoTipo = "conhecimento_tipo";
        private const string EncontradoArquivo = "encontrado_arquivo";
        private const string ConhecimentoTipoArquivo = "conhecimento_tipo_e_arquivo";

        public static List<CampoSchemaSugeridoDto> Sugerir(
            string codigoTipo,
            string? descricaoTipoDocumento,
            ResultadoIdentificacaoDocumentoDto? extracao)
        {
            var campos = ObterCamposBase(codigoTipo, descricaoTipoDocumento);
            var indice = new Dictionary<string, CampoSchemaSugeridoDto>(StringComparer.OrdinalIgnoreCase);
            foreach (var campo in campos)
                Registrar(campo, indice);

            foreach (var campoExtraido in LerCamposExtraidos(extracao))
            {
                var chave = NormalizadorEstrutural.NormalizarChave(campoExtraido.Chave);
                if (string.IsNullOrWhiteSpace(chave))
                    continue;

                var existente = EncontrarPorChaveOuAlias(indice, chave,
                    campoExtraido.RotuloOriginal);
                if (existente != null)
                {
                    existente.EncontradoNoArquivo = true;
                    existente.OrigemSugestao = ConhecimentoTipoArquivo;
                    existente.Confianca = Max(existente.Confianca, campoExtraido.Confianca);
                    AdicionarAlias(existente, campoExtraido.RotuloOriginal);
                    continue;
                }

                var sugerido = CriarCampo(
                    chave,
                    CriarNomeExibicao(chave),
                    InferirTipoDado(campoExtraido),
                    false,
                    indice.Count + 1,
                    EncontradoArquivo,
                    true,
                    campoExtraido.Confianca,
                    campoExtraido.RotuloOriginal);
                Registrar(sugerido, indice);
            }

            return indice.Values
                .OrderBy(x => x.Ordem)
                .Select(NormalizarCampo)
                .ToList();
        }

        public static string ObterNomeTipo(string codigoTipo, string? descricaoTipoDocumento)
        {
            if (!string.IsNullOrWhiteSpace(descricaoTipoDocumento))
                return descricaoTipoDocumento.Trim();

            return codigoTipo switch
            {
                "cnh" => "Carteira Nacional de Habilitacao",
                "conta_luz" => "Conta de luz",
                "contrato" => "Contrato",
                "passaporte" => "Passaporte",
                _ => CriarNomeExibicao(codigoTipo)
            };
        }

        private static List<CampoSchemaSugeridoDto> ObterCamposBase(
            string codigoTipo, string? descricaoTipoDocumento)
        {
            return codigoTipo switch
            {
                "cnh" => Cnh(),
                "conta_luz" or "comprovante_energia" => ContaLuz(),
                "contrato" => Contrato(),
                "passaporte" => Passaporte(),
                _ => Generico(codigoTipo, descricaoTipoDocumento)
            };
        }

        private static List<CampoSchemaSugeridoDto> Cnh() => new()
        {
            Campo("nomeCompleto", "Nome completo", "Nome completo do titular da habilitacao.", "texto", true, 1, "texto_sem_espacos_excedentes", null, "nome", "nome completo", "name"),
            Campo("cpf", "CPF", "Cadastro de Pessoa Fisica do titular.", "texto", false, 2, "somente_digitos", "cpf", "cpf", "registro nacional"),
            Campo("dataNascimento", "Data de nascimento", "Data de nascimento do titular.", "data", true, 3, "data_iso", "data_passada", "data de nascimento", "nascimento", "date of birth"),
            Campo("numeroRegistro", "Numero de registro", "Numero principal de registro da CNH.", "texto", true, 4, "somente_caracteres_alfanumericos", null, "numero de registro", "registro", "driver license number"),
            Campo("categoriaHabilitacao", "Categoria da habilitacao", "Categoria da habilitacao do condutor.", "texto", true, 5, "texto_maiusculo", null, "categoria", "cat hab"),
            Campo("dataPrimeiraHabilitacao", "Data da primeira habilitacao", "Data da primeira habilitacao.", "data", false, 6, "data_iso", null, "primeira habilitacao", "1 habilitacao"),
            Campo("dataEmissao", "Data de emissao", "Data de emissao do documento.", "data", false, 7, "data_iso", null, "emissao", "data emissao"),
            Campo("dataValidade", "Data de validade", "Data de validade da habilitacao.", "data", true, 8, "data_iso", null, "validade", "data de validade", "expiry date"),
            Campo("localEmissao", "Local de emissao", "Local de emissao do documento.", "texto", false, 9, "texto_sem_espacos_excedentes", null, "local emissao"),
            Campo("nomeMae", "Nome da mae", "Nome da mae do titular, quando constar no documento.", "texto", false, 10, "texto_sem_espacos_excedentes", null, "mae", "filiacao mae"),
            Campo("nomePai", "Nome do pai", "Nome do pai do titular, quando constar no documento.", "texto", false, 11, "texto_sem_espacos_excedentes", null, "pai", "filiacao pai"),
            Campo("observacoes", "Observacoes", "Observacoes impressas na CNH.", "texto", false, 12, null, null, "observacoes", "obs")
        };

        private static List<CampoSchemaSugeridoDto> ContaLuz() => new()
        {
            Campo("titular", "Titular", "Nome do titular da unidade consumidora.", "texto", true, 1, "texto_sem_espacos_excedentes", null, "cliente", "titular"),
            Campo("cpfCnpjTitular", "CPF/CNPJ do titular", "Documento fiscal do titular.", "texto", false, 2, "somente_digitos", "cpf_cnpj", "cpf", "cnpj", "cpf cnpj"),
            Campo("endereco", "Endereco", "Endereco completo da unidade consumidora.", "objeto", true, 3, null, null, "endereco").ComFilhos(
                Campo("logradouro", "Logradouro", "Logradouro do endereco.", "texto", false, 1, null, null, "rua", "avenida", "logradouro"),
                Campo("numero", "Numero", "Numero do endereco.", "texto", false, 2, null, null, "numero"),
                Campo("complemento", "Complemento", "Complemento do endereco.", "texto", false, 3, null, null, "complemento"),
                Campo("bairro", "Bairro", "Bairro do endereco.", "texto", false, 4, null, null, "bairro"),
                Campo("cidade", "Cidade", "Cidade do endereco.", "texto", false, 5, null, null, "cidade", "municipio"),
                Campo("estado", "Estado", "Estado ou UF do endereco.", "texto", false, 6, "texto_maiusculo", null, "uf", "estado"),
                Campo("cep", "CEP", "Codigo postal do endereco.", "texto", false, 7, "somente_digitos", "cep", "cep", "codigo postal")),
            Campo("unidadeConsumidora", "Unidade consumidora", "Codigo da unidade consumidora.", "texto", true, 4, "somente_caracteres_alfanumericos", null, "unidade consumidora", "uc"),
            Campo("numeroInstalacao", "Numero da instalacao", "Identificador da instalacao.", "texto", false, 5, "somente_caracteres_alfanumericos", null, "instalacao", "numero instalacao"),
            Campo("mesReferencia", "Mes de referencia", "Mes de referencia da fatura.", "texto", true, 6, null, null, "referencia", "mes referencia"),
            Campo("dataVencimento", "Data de vencimento", "Data de vencimento da fatura.", "data", true, 7, "data_iso", null, "vencimento", "data vencimento"),
            Campo("valorTotal", "Valor total", "Valor total da fatura.", "decimal", true, 8, "moeda", null, "valor total", "total a pagar"),
            Campo("consumoKwh", "Consumo kWh", "Consumo de energia no periodo.", "decimal", false, 9, "decimal", null, "consumo", "kwh", "consumo kwh"),
            Campo("concessionaria", "Concessionaria", "Empresa concessionaria de energia.", "texto", false, 10, "texto_sem_espacos_excedentes", null, "concessionaria", "distribuidora")
        };

        private static List<CampoSchemaSugeridoDto> Contrato() => new()
        {
            Campo("numeroContrato", "Numero do contrato", "Identificador do contrato.", "texto", false, 1, "somente_caracteres_alfanumericos", null, "numero do contrato", "contrato n", "contract number"),
            Campo("tipoContrato", "Tipo de contrato", "Classificacao ou modalidade contratual.", "texto", false, 2, null, null, "tipo contrato"),
            Campo("titulo", "Titulo", "Titulo ou denominacao do contrato.", "texto", false, 3, null, null, "titulo"),
            Campo("partes", "Partes", "Partes envolvidas no contrato.", "lista", true, 4, null, null, "partes").ComItemObjeto(
                Campo("nome", "Nome", "Nome da parte.", "texto", false, 1, null, null, "nome"),
                Campo("papel", "Papel", "Papel da parte no contrato.", "texto", false, 2, null, null, "contratante", "contratada"),
                Campo("documento", "Documento", "Documento da parte.", "texto", false, 3, "somente_caracteres_alfanumericos", null, "cpf", "cnpj")),
            Campo("contratante", "Contratante", "Parte contratante.", "objeto", false, 5, null, null, "contratante").ComFilhos(Campo("nome", "Nome", "Nome do contratante.", "texto", false, 1, null, null, "nome")),
            Campo("contratada", "Contratada", "Parte contratada.", "objeto", false, 6, null, null, "contratada").ComFilhos(Campo("nome", "Nome", "Nome da contratada.", "texto", false, 1, null, null, "nome")),
            Campo("objeto", "Objeto", "Objeto contratual.", "texto", true, 7, null, null, "objeto"),
            Campo("dataAssinatura", "Data de assinatura", "Data de assinatura do contrato.", "data", false, 8, "data_iso", null, "assinatura", "data assinatura"),
            Campo("dataInicioVigencia", "Inicio da vigencia", "Data de inicio da vigencia.", "data", false, 9, "data_iso", null, "inicio vigencia"),
            Campo("dataFimVigencia", "Fim da vigencia", "Data de fim da vigencia.", "data", false, 10, "data_iso", null, "fim vigencia"),
            Campo("prazo", "Prazo", "Prazo contratual.", "texto", false, 11, null, null, "prazo"),
            Campo("valorContrato", "Valor do contrato", "Valor total ou recorrente do contrato.", "decimal", false, 12, "moeda", null, "valor", "valor contrato"),
            Campo("formaPagamento", "Forma de pagamento", "Forma de pagamento pactuada.", "texto", false, 13, null, null, "forma pagamento"),
            Campo("clausulasPrincipais", "Clausulas principais", "Resumo das clausulas principais.", "lista", false, 14, null, null, "clausulas").ComItemTexto(),
            Campo("foro", "Foro", "Foro eleito.", "texto", false, 15, null, null, "foro"),
            Campo("assinaturas", "Assinaturas", "Assinaturas ou representantes signatarios.", "lista", false, 16, null, null, "assinaturas").ComItemObjeto(
                Campo("nome", "Nome", "Nome do signatario.", "texto", false, 1, null, null, "nome"),
                Campo("papel", "Papel", "Papel do signatario.", "texto", false, 2, null, null, "papel"))
        };

        private static List<CampoSchemaSugeridoDto> Passaporte() => new()
        {
            Campo("nomeCompleto", "Nome completo", "Nome completo do titular.", "texto", true, 1, "texto_sem_espacos_excedentes", null, "name", "nome"),
            Campo("sobrenome", "Sobrenome", "Sobrenome do titular.", "texto", false, 2, "texto_sem_espacos_excedentes", null, "surname", "apellido"),
            Campo("nomes", "Nomes", "Nomes do titular.", "texto", false, 3, "texto_sem_espacos_excedentes", null, "given names", "prenombres"),
            Campo("numeroPassaporte", "Numero do passaporte", "Numero principal do passaporte.", "texto", true, 4, "somente_caracteres_alfanumericos", null, "passport no", "passaporte", "numero documento"),
            Campo("nacionalidade", "Nacionalidade", "Nacionalidade do titular.", "texto", true, 5, "codigo_pais_iso3", null, "nationality"),
            Campo("dataNascimento", "Data de nascimento", "Data de nascimento do titular.", "data", true, 6, "data_iso", "data_passada", "date of birth", "birth date"),
            Campo("sexo", "Sexo", "Sexo indicado no documento.", "texto", false, 7, "texto_maiusculo", null, "sex", "sexo"),
            Campo("localNascimento", "Local de nascimento", "Local de nascimento do titular.", "texto", false, 8, null, null, "place of birth"),
            Campo("dataEmissao", "Data de emissao", "Data de emissao do passaporte.", "data", false, 9, "data_iso", null, "date of issue"),
            Campo("dataValidade", "Data de validade", "Data de validade do passaporte.", "data", true, 10, "data_iso", null, "date of expiry", "expiry date"),
            Campo("paisEmissor", "Pais emissor", "Pais emissor em ISO 3166-1 alpha-3 quando disponivel.", "texto", true, 11, "codigo_pais_iso3", null, "issuing country"),
            Campo("autoridadeEmissora", "Autoridade emissora", "Autoridade que emitiu o passaporte.", "texto", false, 12, null, null, "authority", "issuing authority"),
            Campo("mrzLinha1", "MRZ linha 1", "Primeira linha da zona de leitura mecanica.", "texto", false, 13, "texto_maiusculo", "mrz", "mrz line 1"),
            Campo("mrzLinha2", "MRZ linha 2", "Segunda linha da zona de leitura mecanica.", "texto", false, 14, "texto_maiusculo", "mrz", "mrz line 2")
        };

        private static List<CampoSchemaSugeridoDto> Generico(
            string codigoTipo, string? descricaoTipoDocumento)
        {
            var campos = new List<CampoSchemaSugeridoDto>
            {
                Campo("tipoDocumento", "Tipo do documento", "Tipo documental identificado.", "texto", false, 1, null, null, "tipo documento"),
                Campo("titulo", "Titulo", "Titulo ou nome original do documento.", "texto", false, 2, null, null, "titulo"),
                Campo("numeroDocumento", "Numero do documento", "Identificador principal do documento.", "texto", false, 3, "somente_caracteres_alfanumericos", null, "numero", "identificador"),
                Campo("dataEmissao", "Data de emissao", "Data de emissao quando existir.", "data", false, 4, "data_iso", null, "emissao"),
                Campo("dataValidade", "Data de validade", "Data de validade quando existir.", "data", false, 5, "data_iso", null, "validade")
            };

            var contexto = $"{codigoTipo} {descricaoTipoDocumento}".ToLowerInvariant();
            if (contexto.Contains("contrato"))
                campos.AddRange(Contrato().Where(x => campos.All(c => c.Chave != x.Chave)));
            if (contexto.Contains("endereco") || contexto.Contains("residencia") ||
                contexto.Contains("comprovante"))
                campos.AddRange(ContaLuz().Where(x => campos.All(c => c.Chave != x.Chave)));
            return campos.Select((x, i) =>
            {
                x.Ordem = i + 1;
                return x;
            }).ToList();
        }

        private static IEnumerable<CampoExtraidoDocumentoDto> LerCamposExtraidos(
            ResultadoIdentificacaoDocumentoDto? extracao)
        {
            if (extracao?.CamposExtraidos == null)
                return Enumerable.Empty<CampoExtraidoDocumentoDto>();

            return extracao.CamposExtraidos
                .Where(x => !string.IsNullOrWhiteSpace(x.Chave));
        }

        private static CampoSchemaSugeridoDto? EncontrarPorChaveOuAlias(
            IReadOnlyDictionary<string, CampoSchemaSugeridoDto> indice, string chave,
            string? rotuloOriginal)
        {
            if (indice.TryGetValue(chave, out var porChave))
                return porChave;

            var rotulo = NormalizarTexto(rotuloOriginal);
            if (string.IsNullOrWhiteSpace(rotulo))
                return null;

            return indice.Values.FirstOrDefault(c =>
                c.Aliases.Select(NormalizarTexto).Contains(rotulo) ||
                NormalizarTexto(c.NomeExibicao) == rotulo);
        }

        private static void Registrar(CampoSchemaSugeridoDto campo,
            IDictionary<string, CampoSchemaSugeridoDto> indice)
        {
            campo.Chave = NormalizadorEstrutural.NormalizarChave(campo.Chave);
            if (!indice.ContainsKey(campo.Chave))
                indice.Add(campo.Chave, campo);
        }

        private static CampoSchemaSugeridoDto NormalizarCampo(
            CampoSchemaSugeridoDto campo)
        {
            campo.Chave = NormalizadorEstrutural.NormalizarChave(campo.Chave);
            campo.TipoDado = NormalizadorEstrutural.TipoDado(campo.TipoDado);
            campo.OrigemSugestao = NormalizarOrigem(campo.OrigemSugestao);
            campo.Aliases = campo.Aliases
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            campo.CamposFilhos = campo.CamposFilhos.Select(NormalizarCampo).ToList();
            if (campo.ItemLista != null)
                campo.ItemLista = NormalizarCampo(campo.ItemLista);
            return campo;
        }

        private static string NormalizarOrigem(string origem) =>
            origem == ConhecimentoTipo || origem == EncontradoArquivo ||
            origem == ConhecimentoTipoArquivo
                ? origem
                : ConhecimentoTipo;

        private static CampoSchemaSugeridoDto Campo(string chave, string nome,
            string descricao, string tipoDado, bool obrigatorio, int ordem,
            string? regraNormalizacao, string? regraValidacao, params string[] aliases)
        {
            return AplicarDescricao(
                CriarCampo(chave, nome, tipoDado, obrigatorio, ordem,
                    ConhecimentoTipo, false, null, aliases),
                descricao, regraNormalizacao, regraValidacao);
        }

        private static CampoSchemaSugeridoDto CriarCampo(string chave, string nome,
            string tipoDado, bool obrigatorio, int ordem, string origem,
            bool encontrado, decimal? confianca, params string?[] aliases)
        {
            return new CampoSchemaSugeridoDto
            {
                Chave = NormalizadorEstrutural.NormalizarChave(chave),
                NomeExibicao = nome,
                TipoDado = NormalizadorEstrutural.TipoDado(tipoDado),
                ObrigatorioSugerido = obrigatorio,
                OrigemSugestao = origem,
                EncontradoNoArquivo = encontrado,
                Confianca = confianca,
                Ordem = ordem,
                Aliases = aliases
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }

        private static CampoSchemaSugeridoDto AplicarDescricao(
            CampoSchemaSugeridoDto campo, string descricao,
            string? regraNormalizacao, string? regraValidacao)
        {
            campo.Descricao = descricao;
            campo.RegraNormalizacao = regraNormalizacao;
            campo.RegraValidacao = regraValidacao;
            return campo;
        }

        private static CampoSchemaSugeridoDto ComFilhos(
            this CampoSchemaSugeridoDto campo, params CampoSchemaSugeridoDto[] filhos)
        {
            campo.CamposFilhos = filhos.ToList();
            return campo;
        }

        private static CampoSchemaSugeridoDto ComItemObjeto(
            this CampoSchemaSugeridoDto campo, params CampoSchemaSugeridoDto[] filhos)
        {
            campo.ItemLista = new CampoSchemaSugeridoDto
            {
                Chave = "item",
                NomeExibicao = "Item",
                TipoDado = "objeto",
                OrigemSugestao = campo.OrigemSugestao,
                CamposFilhos = filhos.ToList()
            };
            return campo;
        }

        private static CampoSchemaSugeridoDto ComItemTexto(
            this CampoSchemaSugeridoDto campo)
        {
            campo.ItemLista = new CampoSchemaSugeridoDto
            {
                Chave = "item",
                NomeExibicao = "Item",
                TipoDado = "texto",
                OrigemSugestao = campo.OrigemSugestao
            };
            return campo;
        }

        private static void AdicionarAlias(CampoSchemaSugeridoDto campo, string? alias)
        {
            if (string.IsNullOrWhiteSpace(alias) ||
                campo.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase))
                return;
            campo.Aliases.Add(alias.Trim());
        }

        private static string InferirTipoDado(CampoExtraidoDocumentoDto campo)
        {
            var tipo = NormalizadorEstrutural.TipoDado(campo.TipoDado);
            if (!string.IsNullOrWhiteSpace(tipo))
                return tipo;
            return campo.ValorNormalizado switch
            {
                bool => "booleano",
                int or long => "inteiro",
                decimal or double or float => "decimal",
                DateTime => "data_hora",
                _ => "texto"
            };
        }

        private static decimal? Max(decimal? atual, decimal novo) =>
            atual == null || novo > atual ? novo : atual;

        private static string CriarNomeExibicao(string chave)
        {
            var normalizada = NormalizadorEstrutural.NormalizarCodigo(chave)
                .Replace('_', ' ');
            return string.IsNullOrWhiteSpace(normalizada)
                ? chave
                : char.ToUpperInvariant(normalizada[0]) + normalizada[1..];
        }

        private static string NormalizarTexto(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;
            var chars = valor.Trim().ToLowerInvariant()
                .Where(c => char.IsLetter(c) || char.IsWhiteSpace(c))
                .ToArray();
            return string.Join(' ', new string(chars)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }

    public sealed class ValidadorSchemaDocumentoService
        : IValidadorSchemaDocumentoService
    {
        private static readonly HashSet<string> Tipos = new(StringComparer.Ordinal)
        {
            "texto", "inteiro", "decimal", "booleano", "data", "data_hora",
            "objeto", "lista"
        };

        private static readonly HashSet<string> Reservadas = new(StringComparer.Ordinal)
        {
            "tipoDocumento", "validacao", "processamento", "camposAdicionais"
        };

        public ResultadoValidacaoSchemaDto Validar(TipoDocumentoSchemaDto schema)
        {
            var result = new ResultadoValidacaoSchemaDto();
            ValidarCampos(schema.Campos, "$", result, new HashSet<string>());
            result.Valido = result.Erros.Count == 0;
            return result;
        }

        private static void ValidarCampos(IReadOnlyList<CampoSchemaSugeridoDto> campos,
            string path, ResultadoValidacaoSchemaDto result, HashSet<string> pilha)
        {
            var duplicadas = campos.GroupBy(x => x.Chave)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key);
            foreach (var chave in duplicadas)
                result.Erros.Add($"{path}: chave duplicada {chave}.");

            foreach (var campo in campos)
            {
                var chave = NormalizadorEstrutural.NormalizarChave(campo.Chave);
                if (string.IsNullOrWhiteSpace(chave) || !char.IsLetter(chave[0]))
                    result.Erros.Add($"{path}: chave invalida {campo.Chave}.");
                if (Reservadas.Contains(chave))
                    result.Erros.Add($"{path}: chave reservada {chave}.");
                var tipo = NormalizadorEstrutural.TipoDado(campo.TipoDado);
                if (!Tipos.Contains(tipo))
                    result.Erros.Add($"{path}.{chave}: tipo desconhecido {campo.TipoDado}.");
                if (!pilha.Add($"{path}.{chave}"))
                    result.Erros.Add($"{path}.{chave}: estrutura circular.");

                if (tipo == "objeto" && campo.CamposFilhos.Count == 0)
                    result.Erros.Add($"{path}.{chave}: objeto sem campos.");
                if (tipo == "lista" && campo.ItemLista == null)
                    result.Erros.Add($"{path}.{chave}: lista sem definicao de item.");
                if (tipo == "lista" && campo.ItemLista?.TipoDado == "objeto" &&
                    campo.ItemLista.CamposFilhos.Count == 0)
                    result.Erros.Add($"{path}.{chave}: item objeto sem campos.");

                if (campo.CamposFilhos.Count > 0)
                    ValidarCampos(campo.CamposFilhos, $"{path}.{chave}", result, pilha);
                if (campo.ItemLista?.CamposFilhos.Count > 0)
                    ValidarCampos(campo.ItemLista.CamposFilhos, $"{path}.{chave}[]", result, pilha);
                pilha.Remove($"{path}.{chave}");
            }
        }
    }

    public sealed class PublicadorSchemaDocumentoService
        : IPublicadorSchemaDocumentoService
    {
        private readonly AppDbContext _db;
        private readonly IValidadorSchemaDocumentoService _validador;

        public PublicadorSchemaDocumentoService(AppDbContext db,
            IValidadorSchemaDocumentoService validador)
        {
            _db = db;
            _validador = validador;
        }

        public async Task PublicarAsync(Guid tipoDocumentoId, Guid schemaId,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            var schemas = await _db.TipoDocumentoSchemas
                .Include(x => x.Campos)
                .Where(x => x.TipoDocumentoId == tipoDocumentoId)
                .ToListAsync(cancellationToken);
            var schema = schemas.FirstOrDefault(x => x.Id == schemaId) ??
                throw new InvalidOperationException("SCHEMA_NAO_ENCONTRADO");
            if (schema.Status != StatusTipoDocumentoSchema.Rascunho)
                throw new InvalidOperationException("SOMENTE_RASCUNHO_PODE_SER_PUBLICADO");

            var dto = JsonSerializer.Deserialize<TipoDocumentoSchemaDto>(
                schema.SchemaJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            var validacao = _validador.Validar(dto);
            if (!validacao.Valido)
                throw new InvalidOperationException(
                    "SCHEMA_INVALIDO: " + string.Join(" | ", validacao.Erros));

            foreach (var item in schemas)
            {
                if (item.Id == schemaId)
                {
                    item.Status = StatusTipoDocumentoSchema.Ativo;
                    item.Ativo = true;
                    item.PublicadoEm = DateTime.UtcNow;
                    item.AtualizadoEm = DateTime.UtcNow;
                }
                else if (item.Status == StatusTipoDocumentoSchema.Ativo)
                {
                    item.Status = StatusTipoDocumentoSchema.Inativo;
                    item.Ativo = false;
                    item.AtualizadoEm = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
