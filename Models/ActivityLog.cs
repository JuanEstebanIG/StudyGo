using System;

namespace StudyGo.Models
{
    public class ActivityLog
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }

        public User User { get; set; }
    }
}