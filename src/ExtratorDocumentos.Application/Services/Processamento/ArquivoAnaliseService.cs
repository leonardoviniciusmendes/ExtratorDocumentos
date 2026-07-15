using System.Text;
using ExtratorDocumentos.Application.Dtos.Documentos;

namespace ExtratorDocumentos.Application.Services.Processamento
{
    public sealed class ArquivoAnaliseService : IArquivoAnaliseService
    {
        public Task<CaracteristicasArquivo> AnalisarAsync(string nomeOriginal,
            string mimeType, byte[] conteudo, CancellationToken cancellationToken)
        {
            var extensao = Path.GetExtension(nomeOriginal).ToLowerInvariant();
            var tamanho = conteudo.LongLength;
            var textoAproximado = ExtrairTextoAproximado(conteudo, mimeType, extensao);
            var caracteres = textoAproximado.Length;
            var ehImagem = mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            var ehPdf = mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
                extensao.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
            var paginas = ehPdf ? ContarPaginasPdf(conteudo) : ehImagem ? 1 : null;
            var possuiTexto = caracteres >= 80;
            var possuiImagem = ehImagem || (ehPdf && Contem(conteudo, "/Image"));
            var possuiTabela = textoAproximado.Contains("table", StringComparison.OrdinalIgnoreCase) ||
                textoAproximado.Count(x => x == '|') >= 6 ||
                textoAproximado.Count(x => x == ';') >= 4 ||
                textoAproximado.Count(x => x == ',') >= 6;
            var necessitaOcr = ehImagem || (ehPdf && !possuiTexto);
            var qualidade = CalcularQualidade(possuiTexto, caracteres, tamanho, necessitaOcr);

            return Task.FromResult(new CaracteristicasArquivo(
                nomeOriginal,
                extensao,
                string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType,
                tamanho,
                paginas,
                possuiTexto,
                caracteres,
                possuiImagem,
                necessitaOcr,
                possuiTabela,
                qualidade,
                necessitaOcr || possuiImagem));
        }

        private static string ExtrairTextoAproximado(byte[] conteudo, string mimeType, string extensao)
        {
            if (mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
                extensao is ".txt" or ".csv" or ".json" or ".xml")
                return Encoding.UTF8.GetString(conteudo);

            var ascii = new string(conteudo
                .Select(b => b is >= 32 and <= 126 || b is 10 or 13 or 9 ? (char)b : ' ')
                .ToArray());
            return string.Join(' ', ascii.Split(' ',
                StringSplitOptions.RemoveEmptyEntries));
        }

        private static int? ContarPaginasPdf(byte[] conteudo)
        {
            var texto = Encoding.ASCII.GetString(conteudo);
            var count = 0;
            var index = 0;
            while ((index = texto.IndexOf("/Type /Page", index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += 10;
            }
            return count == 0 ? 1 : count;
        }

        private static bool Contem(byte[] conteudo, string valor) =>
            Encoding.ASCII.GetString(conteudo)
                .Contains(valor, StringComparison.OrdinalIgnoreCase);

        private static decimal CalcularQualidade(bool possuiTexto, int caracteres,
            long tamanho, bool necessitaOcr)
        {
            if (necessitaOcr) return 0.45m;
            if (!possuiTexto) return 0.35m;
            if (caracteres > 2000) return 0.95m;
            if (caracteres > 500) return 0.85m;
            return tamanho > 0 ? 0.65m : 0m;
        }
    }
}
