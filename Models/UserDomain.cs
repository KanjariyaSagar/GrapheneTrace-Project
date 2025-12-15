using System.ComponentModel.DataAnnotations;

namespace GrapheneTrace.Models
{
    public class UserDomain
    {
        [Key]
        public string UserId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        // e.g., "Clinician" or "Patient"
        [Required]
        public string Domain { get; set; }
    }
}
