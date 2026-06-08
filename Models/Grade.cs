using System;
using System.Collections.Generic;

namespace StudyGo.Models
{
    public class Grade
    {
        public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public decimal FinalScore { get; set; }
        public DateTime GradedAt { get; set; }

        public Submission Submission { get; set; }
        public ICollection<CriterionEvaluation> CriterionEvaluations { get; set; } = new List<CriterionEvaluation>();
    }
}