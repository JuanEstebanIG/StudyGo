using System;

namespace StudyGo.Models
{
    public class SubmissionVersion
    {
        public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public int VersionNumber { get; set; }
        public string Code { get; set; }
        public DateTime SavedAt { get; set; }

        public Submission Submission { get; set; }
    }
}