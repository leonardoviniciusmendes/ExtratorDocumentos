namespace ExtratorDocumentos.Application.Services.Processamento
{
    public static class DocumentoIdentificacaoPrompt
    {
        public static string Criar(string tipoDocumentoSolicitado, string? schemaJson = null)
        {
            var instrucaoSchema = string.IsNullOrWhiteSpace(schemaJson)
                ? """
                  Voce esta apoiando a criacao de um schema reutilizavel para o tipo documental informado.
                  O arquivo anexado e somente um exemplo desse tipo.
                  Nao limite a analise aos campos encontrados no exemplo.
                  Identifique campos normalmente relevantes e esperados para documentos desse tipo, combinando conhecimento geral, campos visualizados, campos recorrentes, objetos e listas apropriados.
                  Nao inclua valores reais no schema sugerido; preserve valores reais apenas em camposExtraidos da extracao do exemplo.
                  Para cada campo extraido, informe chave canonica, rotulo original quando existir, tipo de dado e confianca.
                  Retorne somente JSON valido.
                  """
                : $$"""
                  O documento foi classificado como: {{tipoDocumentoSolicitado}}.
                  Use obrigatoriamente o schema ativo abaixo como contrato de saida local.
                  Retorne todas as propriedades definidas no schema em camposExtraidos.
                  Quando um valor nao for encontrado, retorne null.
                  Nao adicione propriedades novas dentro de dados; campos nao previstos devem ser identificados separadamente para camposAdicionais.
                  Datas devem ser YYYY-MM-DD. Decimais devem ser numeros JSON, sem simbolo de moeda.
                  Booleanos devem ser true/false. Listas devem ser arrays.
                  Schema ativo:
                  {{schemaJson}}
                  """;

            return $$"""
            Analise todo o conteudo disponivel do documento de identificacao.
            Considere o tipo informado como hipotese/contrato de extracao: {{tipoDocumentoSolicitado}}.
            {{instrucaoSchema}}
            Identifique o tipo efetivo do documento, subtipo quando possivel, pais emissor e idiomas.
            Para categoria identificacao, o documento pode ser brasileiro ou estrangeiro, em qualquer idioma.
            Para categoria endereco, extraia dados de comprovantes ou documentos equivalentes de residencia/endereco.
            Para categoria contrato, extraia partes, datas, objeto e identificadores contratuais quando existirem.
            Preserve valores originais, normalize datas em YYYY-MM-DD e paises em ISO 3166-1 alpha-3 quando a informacao existir.
            Nao invente campos ausentes. Nao declare autenticidade juridica.
            Retorne somente JSON valido, seguindo estritamente esta estrutura:
            {
              "classificacao": {
                "tipoInformado": "string",
                "tipoIdentificado": "string",
                "subtipoIdentificado": null,
                "tipoConfirmado": false,
                "paisEmissor": null,
                "nomeDocumentoOriginal": null,
                "idiomasIdentificados": [],
                "confianca": 0
              },
              "titular": {
                "nomeCompleto": null,
                "sobrenome": null,
                "nomes": null,
                "dataNascimento": null,
                "sexo": null,
                "nacionalidade": null,
                "localNascimento": null
              },
              "identificacao": {
                "numeroDocumento": null,
                "numeroDocumentoNormalizado": null,
                "dataEmissao": null,
                "dataValidade": null,
                "autoridadeEmissora": null,
                "codigoPaisEmissor": null
              },
              "camposEspecificos": {
                "tipoDocumento": "string",
                "valores": {}
              },
              "camposExtraidos": [],
              "validacao": {
                "arquivoLegivel": false,
                "documentoCompleto": false,
                "tipoInformadoConfirmado": false,
                "documentoVencido": null,
                "dadosMinimosEncontrados": false,
                "requerRevisaoHumana": false,
                "confiancaGeral": 0,
                "alertas": [],
                "camposAusentes": [],
                "divergencias": []
              }
            }
            """;
        }
    }
}
