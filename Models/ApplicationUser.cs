using Microsoft.AspNetCore.Identity;

namespace LarSaiVucebte.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string NomeCompleto { get; set; } = string.Empty;
    }
}