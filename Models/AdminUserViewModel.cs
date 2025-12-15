namespace GrapheneTrace.Models
{
    public class AdminUserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public int PatientCount { get; set; }
        public string AssignedClinicianEmail { get; set; }
    }
}
