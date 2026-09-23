namespace LarSaoVicente.Models
{
    // Agrupa medicamentos que têm o mesmo princípio ativo,
    // mesmo com nomes comerciais diferentes (ex.: Glifage e Metformina).
    public class PrincipioAtivo
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        public List<Medicamento> Medicamentos { get; set; } = new();
    }
}