namespace GrapheneTrace.Models
{
    public class SettingsViewModel
    {
        public string DisplayName { get; set; }
        public string PhoneNumber { get; set; }
        public string Timezone { get; set; }
        public string Language { get; set; }

        public string AutoLogout { get; set; }
        public string DataRetention { get; set; }
        public string BackupFrequency { get; set; }

        public string EmailNotifications { get; set; }
        public string SmsAlerts { get; set; }
        public string WeeklySummary { get; set; }

        public string MaintenanceMode { get; set; }
        public string MaxLoginAttempts { get; set; }

        // Password change inputs
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
