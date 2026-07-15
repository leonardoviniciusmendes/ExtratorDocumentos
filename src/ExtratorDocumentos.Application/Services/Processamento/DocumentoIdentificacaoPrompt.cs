namespace ExtratorDocumentos.Application.Services.Processamento
{
    public static class DocumentoIdentificacaoPrompt
    {
        public static string Criar(string tipoDocumentoSolicitado) => $$"""
            Analise todo o conteudo disponivel do documento de identificacao.
            Considere a categoria informada apenas como hipotese: {{tipoDocumentoSolicitado}}.
            Categorias validas recebidas pela API: identificacao, endereco, contrato.
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
