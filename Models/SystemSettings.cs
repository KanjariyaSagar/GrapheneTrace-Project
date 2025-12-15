using System.ComponentModel.DataAnnotations;

namespace GrapheneTrace.Models
{
    public class SystemSettings
    {
        [Key]
        public int Id { get; set; } = 1; // single-row settings

        // User preferences
        public string DisplayName { get; set; }
        public string PhoneNumber { get; set; }
        public string Timezone { get; set; }
        public string Language { get; set; }

        // System behaviour
        public string AutoLogout { get; set; }
        public string DataRetention { get; set; }
        public string BackupFrequency { get; set; }

        // Notifications
        public string EmailNotifications { get; set; }
        public string SmsAlerts { get; set; }
        public string WeeklySummary { get; set; }

        // Security/Maintenance
        public string MaintenanceMode { get; set; }
        public string MaxLoginAttempts { get; set; }
    }
}
