using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrapheneTrace.Models
{
    public class ClinicianPatientAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ClinicianUserId { get; set; }

        [Required]
        public string PatientUserId { get; set; }
    }
}
