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

        // ---------- Regras de leitura ----------

        private const string Unidade = @"(?:MCG|MG|UI|G|ML)\b";

        // Dose: 100MG, 0,5MG, 1.000UI, 450+50MG, 820/12,5MG, 2MG/ML ...
        private static readonly Regex RegexDose = new(
            @"\d+(?:[.,]\d+)*(?:\s*" + Unidade + @"(?:\s*/\s*ML\b)?)?" +
            @"(?:\s*[+/]\s*\d+(?:[.,]\d+)*(?:\s*" + Unidade + @")?)*",
            RegexOptions.IgnoreCase);

        private static readonly Regex RegexTemUnidade =
            new(@"\d\s*" + Unidade, RegexOptions.IgnoreCase);

        // Embalagem: "C/ 30CP", "c/ 30 SACHES", "(60 ACIONAMENTOS)"
        private static readonly Regex RegexEmbalagem =
            new(@"(?:\bC\s*/\s*|\()\s*(\d+)\s*(CP|CPS|SACHES?|ACIONAMENTOS)?", RegexOptions.IgnoreCase);

        private static readonly Regex RegexCorteC = new(@"\bC\s*/\s*\d", RegexOptions.IgnoreCase);
        private static readonly Regex RegexParenteses = new(@"\([^)]*\)");
        private static readonly Regex RegexQuantidade = new(@"^(?<numero>\d+)\s*(?<unidade>[A-Za-z]+)$");

        // Palavras que não ajudam a identificar o medicamento
        private static readonly HashSet<string> PalavrasIgnoradas = new()
        {
            "de", "da", "do", "dos", "das", "e", "ou", "com", "cp", "cps",
            "mg", "mcg", "ml", "ui", "g", "gts", "gotas", "col", "colirio",
            "sache", "saches", "sachet", "envelopes", "xarope", "pomada",
            "creme", "injetavel", "cpr", "capsula", "comprimido", "acionamentos"
        };

        private class EntradaCatalogo
        {
            public HashSet<string> Palavras { get; set; } = new();
            public string PrincipioAtivo { get; set; } = string.Empty;
            public bool PorNomeComercial { get; set; }
        }

        // ---------- Simulação ----------

        public List<ItemImportado> SimularImportacao()
        {
            var caminhoEstoque = Path.Combine(_pastaPlanilhas, "MEDICAMENTOS LSVP 2026 - cópia para projeto.xlsx");
            var caminhoCatalogo = Path.Combine(_pastaPlanilhas, "Catálogo de medicamentos sem nome.xlsx");

            var catalogo = LerCatalogo(caminhoCatalogo);
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

                resultado.Add(InterpretarLinha(nomeOriginal, qtdTexto, catalogo));
            }

            return resultado;
        }

        private static ItemImportado InterpretarLinha(string nomeOriginal, string qtdTexto, List<EntradaCatalogo> catalogo)
        {
            var item = new ItemImportado { NomeOriginal = nomeOriginal };

            // 1) Concentração (dose)
            int? posicaoDose = null;
            foreach (Match m in RegexDose.Matches(nomeOriginal))
            {
                if (RegexTemUnidade.IsMatch(m.Value))
                {
                    item.Concentracao = Regex.Replace(m.Value, @"\s+", "").ToUpperInvariant();
                    posicaoDose = m.Index;
                    break;
                }
            }

            // 2) Nome base: tudo o que vem antes da dose ou do "C/ n"
            int corte = nomeOriginal.Length;
            if (posicaoDose.HasValue)
                corte = Math.Min(corte, posicaoDose.Value);
            var mc = RegexCorteC.Match(nomeOriginal);
            if (mc.Success)
                corte = Math.Min(corte, mc.Index);

            var nomeBase = RegexParenteses.Replace(nomeOriginal.Substring(0, corte), "");
            nomeBase = Regex.Replace(nomeBase, @"\b(COL|GTS)\b", "", RegexOptions.IgnoreCase);
            nomeBase = Regex.Replace(nomeBase, @"\s+", " ").Trim();
            if (nomeBase.Length == 0)
                nomeBase = nomeOriginal;
            item.NomeBase = nomeBase;

            // 3) Tamanho da embalagem e forma
            int? qtdEmbalagem = null;
            string? forma = null;
            var me = RegexEmbalagem.Match(nomeOriginal);
            if (me.Success)
            {
                qtdEmbalagem = int.Parse(me.Groups[1].Value);
                forma = me.Groups[2].Value.ToUpperInvariant() switch
                {
                    "SACHE" or "SACHES" => "Sachê",
                    "ACIONAMENTOS" => "Spray",
                    _ => "Comprimido"
                };
            }

            // 4) Quantidade em estoque (CX = caixas, FR = frascos)
            var maiusculo = nomeOriginal.ToUpperInvariant();
            var mq = RegexQuantidade.Match(qtdTexto);
            if (!mq.Success)
            {
                item.Avisos.Add($"Quantidade em formato inesperado: \"{qtdTexto}\".");
            }
            else
            {
                int numero = int.Parse(mq.Groups["numero"].Value);
                string unidade = mq.Groups["unidade"].Value.ToUpperInvariant();

                if (unidade == "FR")
                {
                    qtdEmbalagem = 1;
                    if (maiusculo.Contains("ACIONAMENTOS")) forma = "Spray";
                    else if (Regex.IsMatch(maiusculo, @"\bCOL\b")) forma = "Colírio";
                    else if (Regex.IsMatch(maiusculo, @"\b(GTS|GOTAS)\b")) forma = "Gotas";
                    else forma = "Frasco";
                    item.EstoqueInicialCalculado = numero;
                }
                else if (unidade == "CX")
                {
                    if (qtdEmbalagem.HasValue)
                        item.EstoqueInicialCalculado = numero * qtdEmbalagem.Value;
                    else
                        item.Avisos.Add("Não foi possível identificar o tamanho da embalagem (\"C/ n\"). Revisar manualmente.");
                }
                else
                {
                    item.Avisos.Add($"Unidade desconhecida: \"{unidade}\". Revisar manualmente.");
                }
            }

            item.Forma = forma;
            item.QtdPorEmbalagem = qtdEmbalagem;

            // 5) Princípio ativo, procurado no catálogo
            item.PrincipioAtivoEncontrado = BuscarPrincipioAtivo(nomeBase, catalogo);
            if (item.PrincipioAtivoEncontrado == null)
                item.Avisos.Add("Princípio ativo não encontrado no catálogo. Revisar manualmente.");

            return item;
        }

        // ---------- Catálogo e busca do princípio ativo ----------

        private static List<EntradaCatalogo> LerCatalogo(string caminho)
        {
            var lista = new List<EntradaCatalogo>();
            using var workbook = new XLWorkbook(caminho);
            var planilha = workbook.Worksheet(1);
            var ultimaLinha = planilha.LastRowUsed()!.RowNumber();

            for (int linha = 2; linha <= ultimaLinha; linha++)
            {
                var prescrito = planilha.Cell(linha, 1).GetString().Trim();
                var generico = planilha.Cell(linha, 2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(generico))
                    continue;

                // Pelo nome comercial (coluna 1), ex.: "Glifage XR 500mg" -> Metformina
                if (!string.IsNullOrWhiteSpace(prescrito))
                {
                    lista.Add(new EntradaCatalogo
                    {
                        Palavras = ExtrairPalavras(prescrito),
                        PrincipioAtivo = generico,
                        PorNomeComercial = true
                    });
                }

                // Pelo próprio nome do princípio ativo (coluna 2)
                lista.Add(new EntradaCatalogo
                {
                    Palavras = ExtrairPalavras(generico),
                    PrincipioAtivo = generico,
                    PorNomeComercial = false
                });
            }

            return lista;
        }

        // Só aceita se TODAS as palavras do nome estiverem no candidato.
        // Assim "Ácido Fólico" não casa com "Ácido Ascórbico".
        private static string? BuscarPrincipioAtivo(string nomeBase, List<EntradaCatalogo> catalogo)
        {
            var palavras = ExtrairPalavras(nomeBase);
            if (palavras.Count == 0)
                return null;

            string? melhor = null;
            int melhorPontos = int.MinValue;

            foreach (var entrada in catalogo)
            {
                if (entrada.Palavras.Count == 0)
                    continue;

                if (!palavras.All(p => entrada.Palavras.Any(c => PalavraCombina(p, c))))
                    continue;

                // Quanto menos palavras sobrando, mais parecido; nome comercial desempata.
                int extras = entrada.Palavras.Count - palavras.Count;
                int pontos = -Math.Abs(extras) * 10 + (entrada.PorNomeComercial ? 5 : 0);

                if (pontos > melhorPontos)
                {
                    melhorPontos = pontos;
                    melhor = entrada.PrincipioAtivo;
                }
            }

            return melhor;
        }

        private static bool PalavraCombina(string palavra, string candidata)
        {
            // "vit" combina com "vitamina", mas só a partir de 3 letras
            return palavra == candidata ||
                   (palavra.Length >= 3 && candidata.StartsWith(palavra, StringComparison.Ordinal));
        }

        private static HashSet<string> ExtrairPalavras(string texto)
        {
            var semParenteses = RegexParenteses.Replace(texto, " ");
            var resultado = new HashSet<string>();

            foreach (Match m in Regex.Matches(Normalizar(semParenteses), "[a-z0-9]+"))
            {
                var palavra = m.Value;
                if (char.IsDigit(palavra[0])) continue;              // números e doses
                if (PalavrasIgnoradas.Contains(palavra)) continue;   // "de", "mg", "col"...
                resultado.Add(palavra);
            }

            return resultado;
        }

        private static string Normalizar(string texto)
        {
            var normalizado = texto.Normalize(NormalizationForm.FormD);
            var semAcentos = new StringBuilder();
            foreach (var c in normalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    semAcentos.Append(c);
            }
            return semAcentos.ToString().ToLowerInvariant().Trim();
        }
    }
}