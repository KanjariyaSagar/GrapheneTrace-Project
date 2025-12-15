using System;

namespace GrapheneTrace.Models
{
    public class LogEntryViewModel
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Raw { get; set; }
    }
}
