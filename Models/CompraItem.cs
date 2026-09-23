namespace LarSaoVicente.Models
{
    public class CompraItem
    {
        public int Id { get; set; }
        public int Quantidade { get; set; }

        public int CompraId { get; set; }
        public Compra? Compra { get; set; }

        // Cada item de compra gera um lote novo (com a validade informada na compra)
        public int LoteId { get; set; }
        public Lote? Lote { get; set; }
    }
}