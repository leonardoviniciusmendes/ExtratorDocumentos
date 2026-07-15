using Microsoft.Extensions.Configuration;
using ExtratorDocumentos.Domain;

namespace ExtratorDocumentos.Infrastructure.Storage
{
    public class LocalStorageService
    {
        private readonly string _basePath;

        public LocalStorageService(IConfiguration configuration)
        {
            _basePath = Path.GetFullPath(configuration["Storage:Local:BasePath"] ?? "storage");
            Directory.CreateDirectory(_basePath);
        }

        public async Task SalvarAsync(string chave, Stream conteudo, CancellationToken cancellationToken)
        {
            var caminho = ObterCaminhoSeguro(chave);
            Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
            await using var arquivo = File.Create(caminho);
            await conteudo.CopyToAsync(arquivo, cancellationToken);
        }

        public Task<Stream> AbrirLeituraAsync(string chave, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Stream stream = File.OpenRead(ObterCaminhoSeguro(chave));
            return Task.FromResult(stream);
        }

        public Task<bool> ExisteAsync(string chave, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(File.Exists(ObterCaminhoSeguro(chave)));
        }

        private string ObterCaminhoSeguro(string chave)
        {
            var caminho = Path.GetFullPath(Path.Combine(_basePath, chave.Replace('/', Path.DirectorySeparatorChar)));
            if (!caminho.StartsWith(_basePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chave de storage invalida.");
            return caminho;
        }
    }

    public static class StorageKeyBuilder
    {
        public static string CriarChaveOriginal(
            Documento documento, int versao, string nomeArquivo)
        {
            var extensao = Path.GetExtension(Path.GetFileName(nomeArquivo)).ToLowerInvariant();
            var nome = $"{documento.Id:N}-v{versao}{extensao}";
            return $"{CriarPrefixo(documento)}/original/{nome}";
        }

        public static string CriarChaveDerivada(
            string chaveOriginal, string pasta, string extensao)
        {
            var segmentos = chaveOriginal.Replace('\\', '/').Split('/');
            var indiceOriginal = Array.FindLastIndex(segmentos,
                x => string.Equals(x, "original", StringComparison.OrdinalIgnoreCase));
            if (indiceOriginal < 0)
                throw new InvalidOperationException(
                    "A chave original nao pertence a nova estrutura de storage.");

            var nomeBase = Path.GetFileNameWithoutExtension(segmentos[^1]);
            return $"{string.Join('/', segmentos.Take(indiceOriginal))}/{pasta}/{nomeBase}{extensao}";
        }

        public static bool EhChaveEstruturada(string chave) =>
            chave.Replace('\\', '/').StartsWith("clientes/", StringComparison.OrdinalIgnoreCase);

        private static string CriarPrefixo(Documento documento)
        {
            var tipo = NormalizarTipoDocumento(documento.Tipo);
            var cpfTitular = SomenteDigitosOuNulo(documento.Cpf, 11);
            if (cpfTitular == null)
                return $"pendentes/{documento.Id:N}/{tipo}";

            return documento.Papel switch
            {
                PapelDocumento.Titular =>
                    $"clientes/{cpfTitular}/titular/{tipo}",
                PapelDocumento.Dependente =>
                    $"clientes/{cpfTitular}/dependentes/{IdentificadorDependente(documento)}/{tipo}",
                PapelDocumento.Empresa =>
                    $"clientes/{cpfTitular}/empresa/{SomenteDigitos(documento.Cnpj, 14, nameof(documento.Cnpj))}/{tipo}",
                _ => throw new InvalidOperationException("Papel de documento invalido.")
            };
        }

        private static string IdentificadorDependente(Documento documento) =>
            SomenteDigitosOuNulo(documento.CpfDependente, 11) ?? $"sem-cpf/{documento.Id:N}";

        private static string SomenteDigitos(string? valor, int tamanho, string campo)
        {
            var normalizado = new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
            if (normalizado.Length != tamanho)
                throw new InvalidOperationException(
                    $"{campo} deve conter {tamanho} digitos.");
            return normalizado;
        }

        private static string? SomenteDigitosOuNulo(string? valor, int tamanho)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var normalizado = new string(valor.Where(char.IsDigit).ToArray());
            return normalizado.Length == tamanho ? normalizado : null;
        }

        private static string NormalizarTipoDocumento(TipoDocumentoLegado tipo) => tipo switch
        {
            TipoDocumentoLegado.RG => "rg",
            TipoDocumentoLegado.CPF => "cpf",
            TipoDocumentoLegado.CNH => "cnh",
            TipoDocumentoLegado.ComprovanteResidencia => "comprovante-residencia",
            TipoDocumentoLegado.ContaLuz => "conta-luz",
            TipoDocumentoLegado.CertidaoNascimento => "certidao-nascimento",
            TipoDocumentoLegado.CertidaoCasamento => "certidao-casamento",
            TipoDocumentoLegado.ContratoSocial => "contrato-social",
            TipoDocumentoLegado.CartaoCNPJ => "cartao-cnpj",
            TipoDocumentoLegado.ContaAgua => "conta-agua",
            TipoDocumentoLegado.ContaTelefone => "conta-telefone",
            TipoDocumentoLegado.ContaInternet => "conta-internet",
            TipoDocumentoLegado.ContaGas => "conta-gas",
            TipoDocumentoLegado.FaturaCartaoCredito => "fatura-cartao-credito",
            TipoDocumentoLegado.ExtratoBancario => "extrato-bancario",
            TipoDocumentoLegado.ContratoLocacao => "contrato-locacao",
            TipoDocumentoLegado.IPTU => "iptu",
            TipoDocumentoLegado.Elegibilidade => "elegibilidade",
            TipoDocumentoLegado.FichaAssociativa => "ficha-associativa",
            TipoDocumentoLegado.DocumentoOficialComSelfie => "documento-oficial-com-selfie",
            _ => "outros"
        };
    }
}
