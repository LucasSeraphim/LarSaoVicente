namespace LarSaoVicente.Models
{
    public class Medicamento
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Concentracao { get; set; }   // ex.: "50mg"
        public string? Forma { get; set; }           // ex.: "Comprimido", "Frasco", "Ampola"
        public int QtdPorEmbalagem { get; set; }      // ex.: 30 (comprimidos por caixa)
        public int EstoqueMinimo { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        public int PrincipioAtivoId { get; set; }
        public PrincipioAtivo? PrincipioAtivo { get; set; }

        public List<Lote> Lotes { get; set; } = new();
    }
}