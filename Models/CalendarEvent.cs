using System;

namespace StudyGo.Models
{
    public class CalendarEvent
    {
        public Guid Id { get; set; }
        public Guid CourseId { get; set; }
        public string Title { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }

        public Course Course { get; set; }
    }
}