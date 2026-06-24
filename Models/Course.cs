using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StudyGo.Models
{
    public class Course
    {
        public Guid Id { get; set; }
        public Guid InstitutionId { get; set; }
        public Guid TeacherId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }

        public Institution Institution { get; set; }
        public User Teacher { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
        public ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
        public ICollection<DriveFile> DriveFiles { get; set; } = new List<DriveFile>();
    }
}
