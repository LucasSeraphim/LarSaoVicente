using LarSaoVicente.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;    


namespace LarSaoVicente.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<PrincipioAtivo> PrincipiosAtivos { get; set; } = null!;
        public DbSet<Medicamento> Medicamentos { get; set; } = null!;
        public DbSet<Lote> Lotes { get; set; } = null!;
        public DbSet<Fornecedor> Fornecedores { get; set; } = null!;
        public DbSet<Compra> Compras { get; set; } = null!;
        public DbSet<CompraItem> ComprasItens { get; set; } = null!;
        public DbSet<Movimentacao> Movimentacoes { get; set; } = null!;
    }
}