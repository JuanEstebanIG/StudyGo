using System;
using System.Collections.Generic;
using StudyGo.Enums;

namespace StudyGo.Models
{
    public class Submission
    {
        public Guid Id { get; set; }
        public Guid ProgrammingTaskId { get; set; }
        public Guid StudentId { get; set; }
        public SubmissionStatus Status { get; set; }

        public ProgrammingTask ProgrammingTask { get; set; }
        public User Student { get; set; }
        public ICollection<SubmissionVersion> Versions { get; set; } = new List<SubmissionVersion>();
        public Grade Grade { get; set; }
    }
}