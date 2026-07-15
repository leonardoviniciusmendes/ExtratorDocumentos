namespace ExtratorDocumentos.Application.Services.Processamento
{
    public static class DocumentoIdentificacaoPrompt
    {
        public static string Criar(string tipoDocumentoSolicitado)
        {
            if (string.IsNullOrWhiteSpace(tipoDocumentoSolicitado))
            {
                throw new ArgumentException(
                    "O tipo de documento deve ser informado.",
                    nameof(tipoDocumentoSolicitado));
            }

            var tipoNormalizado = tipoDocumentoSolicitado
                .Trim()
                .ToUpperInvariant();

            return tipoNormalizado switch
            {
                "IDENTIFICACAO" or "IDENTIFICAÇÃO" => CriarPromptIdentificacao(),
                "ENDERECO" or "ENDEREÇO" => CriarPromptEndereco(),
                "CONTRATO" => CriarPromptContrato(),

                _ => throw new ArgumentException(
                    $"Tipo de documento não suportado: {tipoDocumentoSolicitado}. " +
                    "Tipos permitidos: Identificacao, Endereco e Contrato.",
                    nameof(tipoDocumentoSolicitado))
            };
        }

        private static string CriarPromptIdentificacao() => """
            Analise integralmente o documento enviado.

            O documento pertence à categoria IDENTIFICACAO.

            Identifique o tipo efetivo do documento, mesmo que ele esteja em outro idioma
            ou tenha sido emitido fora do Brasil.

            Tipos nacionais possíveis incluem, entre outros:

            - Carteira de Identidade Nacional (CIN)
            - Registro Geral (RG)
            - Carteira Nacional de Habilitação (CNH)
            - Passaporte brasileiro
            - Registro Nacional Migratório (RNM)
            - Registro Nacional de Estrangeiro (RNE)
            - Carteira de Trabalho e Previdência Social (CTPS)
            - Carteira profissional
            - Carteira funcional
            - Identidade militar
            - Certificado de reservista
            - CPF
            - Título de eleitor

            Tipos internacionais possíveis incluem, entre outros:

            - Passport
            - Diplomatic Passport
            - Official Passport
            - Emergency Passport
            - Temporary Passport
            - National Identity Card
            - Identity Card
            - Citizen Card
            - Personal Identity Card
            - DNI
            - Cédula de Identidad
            - Cédula de Ciudadanía
            - Cartão de Cidadão
            - Residence Permit
            - Residence Card
            - Permanent Resident Card
            - Temporary Resident Card
            - Alien Registration Card
            - Green Card
            - Driver License
            - Driving Licence
            - International Driving Permit
            - Military ID
            - Diplomatic Identity Card
            - Consular Identity Card
            - Refugee Identity Card
            - Refugee Travel Document
            - Stateless Person Travel Document
            - Seafarer's Identity Document
            - Outro documento oficial de identificação

            Regras obrigatórias:

            - Analise todas as páginas, lados, imagens e textos disponíveis.
            - Preserve os valores exatamente como aparecem no documento.
            - Não invente informações.
            - Quando uma informação não existir ou não estiver legível, retorne null.
            - Normalize datas válidas no formato YYYY-MM-DD.
            - Informe países no padrão ISO 3166-1 alpha-3 quando possível.
            - Diferencie nome completo, sobrenome e demais nomes quando possível.
            - Não confunda número do documento com número de formulário, controle,
              registro interno ou código de segurança.
            - Não declare autenticidade jurídica do documento.
            - Retorne somente JSON válido.
            - Não retorne markdown, explicações, comentários ou texto fora do JSON.

            Retorne exatamente esta estrutura:

            {
              "classificacao": {
                "tipoInformado": "Identificacao",
                "tipoIdentificado": null,
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
                "cpf": null,
                "rg": null,
                "cnh": null,
                "passaporte": null,
                "rne": null,
                "rnm": null,
                "dataEmissao": null,
                "dataValidade": null,
                "autoridadeEmissora": null,
                "orgaoEmissor": null,
                "ufEmissora": null,
                "codigoPaisEmissor": null
              },
              "camposEspecificos": {
                "tipoDocumento": null,
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

        private static string CriarPromptEndereco() => """
            Analise integralmente o documento enviado.

            O documento pertence à categoria ENDERECO.

            Identifique o tipo de comprovante apresentado, o titular, o endereço completo,
            a entidade emissora e as datas encontradas.

            Exemplos de documentos aceitos:

            - Conta de energia elétrica
            - Conta de água
            - Conta de gás
            - Conta de telefone
            - Conta de internet
            - Fatura de cartão de crédito
            - Extrato bancário
            - Contrato de aluguel
            - Declaração de residência
            - Correspondência de órgão público
            - Documento fiscal contendo endereço
            - Comprovante de residência estrangeiro
            - Utility Bill
            - Bank Statement
            - Council Tax Bill
            - Tenancy Agreement
            - Outro documento equivalente

            Regras obrigatórias:

            - Analise todas as páginas disponíveis.
            - Preserve os valores originais.
            - Não invente informações.
            - Quando uma informação não existir ou não estiver legível, retorne null.
            - Normalize datas válidas no formato YYYY-MM-DD.
            - Normalize o país no padrão ISO 3166-1 alpha-3 quando possível.
            - Não considere endereço de cobrança secundário como endereço principal
              sem evidência suficiente.
            - Retorne somente JSON válido.
            - Não retorne markdown, comentários ou explicações.

            Retorne exatamente esta estrutura:

            {
              "classificacao": {
                "tipoInformado": "Endereco",
                "tipoIdentificado": null,
                "subtipoIdentificado": null,
                "tipoConfirmado": false,
                "paisEmissor": null,
                "nomeDocumentoOriginal": null,
                "idiomasIdentificados": [],
                "confianca": 0
              },
              "titular": {
                "nomeCompleto": null,
                "documento": null
              },
              "endereco": {
                "logradouro": null,
                "numero": null,
                "complemento": null,
                "bairro": null,
                "cidade": null,
                "estado": null,
                "cep": null,
                "pais": null,
                "codigoPais": null,
                "enderecoCompletoOriginal": null
              },
              "documento": {
                "emissor": null,
                "numeroCliente": null,
                "numeroDocumento": null,
                "dataEmissao": null,
                "dataVencimento": null,
                "periodoReferencia": null
              },
              "camposEspecificos": {
                "tipoDocumento": null,
                "valores": {}
              },
              "camposExtraidos": [],
              "validacao": {
                "arquivoLegivel": false,
                "documentoCompleto": false,
                "tipoInformadoConfirmado": false,
                "enderecoEncontrado": false,
                "titularEncontrado": false,
                "documentoRecente": null,
                "dadosMinimosEncontrados": false,
                "requerRevisaoHumana": false,
                "confiancaGeral": 0,
                "alertas": [],
                "camposAusentes": [],
                "divergencias": []
              }
            }
            """;

        private static string CriarPromptContrato() => """
            Analise integralmente o contrato enviado.

            O documento pertence à categoria CONTRATO.

            Identifique as partes, objeto, números contratuais, vigência, valores,
            cláusulas relevantes, assinaturas e demais dados disponíveis.

            Regras obrigatórias:

            - Analise todas as páginas, anexos e aditivos disponíveis.
            - Preserve os valores originais.
            - Não invente informações.
            - Quando uma informação não existir ou não estiver legível, retorne null.
            - Normalize datas válidas no formato YYYY-MM-DD.
            - Valores monetários devem ser retornados como números decimais,
              sem símbolos de moeda e sem separadores de milhar.
            - Informe separadamente contratante e contratado quando possível.
            - Não declare validade, autenticidade ou eficácia jurídica.
            - Não forneça aconselhamento jurídico.
            - Retorne somente JSON válido.
            - Não retorne markdown, comentários ou explicações.

            Retorne exatamente esta estrutura:

            {
              "classificacao": {
                "tipoInformado": "Contrato",
                "tipoIdentificado": null,
                "subtipoIdentificado": null,
                "tipoConfirmado": false,
                "paisEmissor": null,
                "nomeDocumentoOriginal": null,
                "idiomasIdentificados": [],
                "confianca": 0
              },
              "contrato": {
                "numeroContrato": null,
                "numeroProposta": null,
                "objeto": null,
                "dataAssinatura": null,
                "dataInicioVigencia": null,
                "dataFimVigencia": null,
                "prazo": null,
                "valorTotal": null,
                "moeda": null,
                "formaPagamento": null,
                "foro": null
              },
              "partes": [],
              "assinaturas": [],
              "clausulasRelevantes": [],
              "identificadores": {},
              "camposEspecificos": {
                "tipoDocumento": null,
                "valores": {}
              },
              "camposExtraidos": [],
              "validacao": {
                "arquivoLegivel": false,
                "documentoCompleto": false,
                "tipoInformadoConfirmado": false,
                "partesEncontradas": false,
                "objetoEncontrado": false,
                "assinaturasEncontradas": false,
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