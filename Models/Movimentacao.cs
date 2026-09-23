namespace LarSaoVicente.Models
{
    public enum TipoMovimentacao
    {
        Entrada,
        Saida
    }

    public class Movimentacao
    {
        public int Id { get; set; }
        public TipoMovimentacao Tipo { get; set; }
        public int Quantidade { get; set; }
        public DateTime DataHora { get; set; } = DateTime.Now;
        public string? MotivoOuDestino { get; set; }  // ex.: "Enfermaria 2", "Ajuste de contagem"

        public int LoteId { get; set; }
        public Lote? Lote { get; set; }

        public string? UsuarioId { get; set; }
        public ApplicationUser? Usuario { get; set; }
    }
}