namespace LarSaoVicente.Models
{
    public class Fornecedor
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Contato { get; set; }

        public List<Compra> Compras { get; set; } = new();
    }
}