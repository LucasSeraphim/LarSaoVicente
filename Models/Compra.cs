namespace LarSaoVicente.Models
{
    public class Compra
    {
        public int Id { get; set; }
        public DateTime Data { get; set; } = DateTime.Now;
        public string? Observacao { get; set; }

        public int FornecedorId { get; set; }
        public Fornecedor? Fornecedor { get; set; }

        public string? UsuarioId { get; set; }  // quem registrou (Identity usa string como Id)
        public ApplicationUser? Usuario { get; set; }

        public List<CompraItem> Itens { get; set; } = new();
    }
}