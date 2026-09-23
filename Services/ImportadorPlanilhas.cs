namespace LarSaoVicente.ViewModels
{
    public class ItemImportado
    {
        public string NomeOriginal { get; set; } = string.Empty;
        public string? NomeBase { get; set; }
        public string? Concentracao { get; set; }
        public string? Forma { get; set; }
        public int? QtdPorEmbalagem { get; set; }
        public int? EstoqueInicialCalculado { get; set; }
        public string? PrincipioAtivoEncontrado { get; set; }
        public List<string> Avisos { get; set; } = new();
    }
}