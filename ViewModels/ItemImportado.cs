using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using LarSaoVicente.ViewModels;

namespace LarSaoVicente.Services
{
    public class ImportadorPlanilhas
    {
        private readonly string _pastaPlanilhas;

        public ImportadorPlanilhas(string pastaPlanilhas)
        {
            _pastaPlanilhas = pastaPlanilhas;
        }

        public List<ItemImportado> SimularImportacao()
        {
            var caminhoEstoque = Path.Combine(_pastaPlanilhas, "MEDICAMENTOS LSVP 2026 - cópia para projeto.xlsx");
            var caminhoCatalogo = Path.Combine(_pastaPlanilhas, "Catálogo de medicamentos sem nome.xlsx");

            var ativosDoCatalogo = LerPrincipiosAtivosDoCatalogo(caminhoCatalogo);

            var resultado = new List<ItemImportado>();

            using var workbook = new XLWorkbook(caminhoEstoque);
            var planilha = workbook.Worksheet(1);
            var ultimaLinha = planilha.LastRowUsed()!.RowNumber();

            for (int linha = 2; linha <= ultimaLinha; linha++)
            {
                var nomeOriginal = planilha.Cell(linha, 1).GetString().Trim();
                var qtdTexto = planilha.Cell(linha, 2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(nomeOriginal))
                    continue;

                var item = InterpretarLinha(nomeOriginal, qtdTexto, ativosDoCatalogo);
                resultado.Add(item);
            }

            return resultado;
        }

        private ItemImportado InterpretarLinha(string nomeOriginal, string qtdTexto, List<string> ativosDoCatalogo)
        {
            var item = new ItemImportado { NomeOriginal = nomeOriginal };

            var match = Regex.Match(nomeOriginal,
                @"^(?<nome>.+?)\s+(?<dose>\d+[\.,]?\d*)\s*(?<unidadeDose>MG|MCG|G|ML)\b(.*?C\/\s*(?<qtdEmb>\d+)\s*(?<forma>CP|SACHES|ACIONAMENTOS)?)?",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                item.NomeBase = match.Groups["nome"].Value.Trim();
                item.Concentracao = match.Groups["dose"].Value + match.Groups["unidadeDose"].Value.ToUpper();

                if (match.Groups["qtdEmb"].Success)
                {
                    item.QtdPorEmbalagem = int.Parse(match.Groups["qtdEmb"].Value);
                    item.Forma = match.Groups["forma"].Success
                        ? TraduzirForma(match.Groups["forma"].Value)
                        : "Comprimido";
                }
                else
                {
                    item.Avisos.Add("Não foi possível identificar o tamanho da embalagem (\"C/ n\"). Revisar manualmente.");
                }
            }
            else
            {
                item.NomeBase = nomeOriginal;
                item.Avisos.Add("Não foi possível separar nome, dose e embalagem automaticamente. Revisar manualmente.");
            }

            var matchQtd = Regex.Match(qtdTexto, @"^(?<numero>\d+)(?<unidade>[A-Za-z]+)$");
            if (matchQtd.Success && item.QtdPorEmbalagem.HasValue)
            {
                int numeroEmbalagens = int.Parse(matchQtd.Groups["numero"].Value);
                item.EstoqueInicialCalculado = numeroEmbalagens * item.QtdPorEmbalagem.Value;
            }
            else if (!matchQtd.Success)
            {
                item.Avisos.Add($"Quantidade em formato inesperado: \"{qtdTexto}\".");
            }

            item.PrincipioAtivoEncontrado = BuscarPrincipioAtivo(item.NomeBase ?? nomeOriginal, ativosDoCatalogo);
            if (item.PrincipioAtivoEncontrado == null)
                item.Avisos.Add("Princípio ativo não encontrado no catálogo. Revisar manualmente.");

            return item;
        }

        private string TraduzirForma(string codigo) => codigo.ToUpper() switch
        {
            "CP" => "Comprimido",
            "SACHES" => "Sachê",
            "ACIONAMENTOS" => "Spray",
            _ => codigo
        };

        private List<string> LerPrincipiosAtivosDoCatalogo(string caminho)
        {
            var lista = new List<string>();
            using var workbook = new XLWorkbook(caminho);
            var planilha = workbook.Worksheet(1);
            var ultimaLinha = planilha.LastRowUsed()!.RowNumber();

            for (int linha = 2; linha <= ultimaLinha; linha++)
            {
                var ativo = planilha.Cell(linha, 2).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(ativo) && !lista.Contains(ativo, StringComparer.OrdinalIgnoreCase))
                    lista.Add(ativo);
            }
            return lista;
        }

        private string? BuscarPrincipioAtivo(string nomeBase, List<string> ativosDoCatalogo)
        {
            var primeiraPalavra = Normalizar(nomeBase.Split(' ', '/', '(')[0]);

            foreach (var ativo in ativosDoCatalogo)
            {
                if (Normalizar(ativo).Contains(primeiraPalavra) || primeiraPalavra.Contains(Normalizar(ativo).Split(' ')[0]))
                    return ativo;
            }
            return null;
        }

        private string Normalizar(string texto)
        {
            var normalizado = texto.Normalize(NormalizationForm.FormD);
            var semAcentos = new StringBuilder();
            foreach (var c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    semAcentos.Append(c);
            }
            return semAcentos.ToString().ToLower().Trim();
        }
    }
}