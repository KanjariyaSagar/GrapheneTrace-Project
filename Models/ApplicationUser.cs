using Microsoft.AspNetCore.Identity;

namespace GrapheneTrace.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Custom fields
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
