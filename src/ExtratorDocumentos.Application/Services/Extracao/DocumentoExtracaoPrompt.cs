using ExtratorDocumentos.Domain;

namespace ExtratorDocumentos.Application.Services.Extracao
{
    internal static class DocumentoExtracaoPrompt
    {
        public static string Criar(TipoDocumento tipo) => $"""
            Extraia os dados de identificacao deste documento brasileiro do tipo {tipo}.
            Responda somente JSON valido, sem markdown. Use null quando ausente.
            Datas em yyyy-MM-dd. CPF/CNPJ somente digitos. confianca entre 0 e 100.
            Campos: nomeCompleto, cpf, rg, orgaoEmissor, ufEmissao, dataNascimento,
            naturalidade, nacionalidade, nomeMae, nomePai, numeroCnh, categoriaCnh,
            validadeCnh, dataPrimeiraHabilitacao, dataEmissao, localEmissao,
            numeroRenach, observacoesCnh, cnpj, razaoSocial, nomeFantasia, endereco,
            nomeTitularEndereco, logradouro, numeroEndereco, complemento, bairro,
            cidade, estado, cep, emissorDocumento, numeroCliente, numeroInstalacao,
            mesReferencia, dataVencimento,
            matriculaCertidao, livro, folha, termo, confianca.
            Para CNH, numeroCnh e o numero de registro. Nao confunda com CPF,
            numero do formulario, espelho ou RENACH. Extraia frente e verso quando existirem.
            Para comprovantes de endereco, separe o endereco completo nos campos estruturados.
            CEP deve conter somente 8 digitos. mesReferencia deve preservar mes e ano exibidos.
            O titular do endereco pode ser diferente da pessoa relacionada ao documento.
            """;
    }
}
