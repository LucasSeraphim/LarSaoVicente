namespace LarSaoVicente.Models
{
    public class Lote
    {
        public int Id { get; set; }
        public string NumeroLote { get; set; } = string.Empty;
        public DateTime Validade { get; set; }

        public int MedicamentoId { get; set; }
        public Medicamento? Medicamento { get; set; }

        public List<Movimentacao> Movimentacoes { get; set; } = new();

        // Não gravamos "quantidade atual" aqui: o saldo é sempre
        // calculado somando as movimentações de entrada e subtraindo as de saída.
    }
}
