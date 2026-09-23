using Microsoft.AspNetCore.Identity;

namespace LarSaoVicente.Models
{    public class ApplicationUser : IdentityUser
    {
        public string NomeCompleto { get; set; } = string.Empty;
    }
}