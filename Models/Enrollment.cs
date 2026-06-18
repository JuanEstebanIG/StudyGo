using System;
using StudyGo.Enums;

namespace StudyGo.Models
{
    public class Enrollment
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public Guid EnrolledBy { get; set; }
        public DateTime EnrolledAt { get; set; }
        public EnrollmentStatus Status { get; set; }

        public User Student { get; set; }
        public Course Course { get; set; }
    }
}
