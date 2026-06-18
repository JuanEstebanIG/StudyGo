using System;
using System.Collections.Generic;

namespace StudyGo.Models
{
    public class RubricCriteria
    {
        public Guid Id { get; set; }
        public Guid RubricId { get; set; }
        public string Description { get; set; }
        public decimal Weight { get; set; }

        public Rubric Rubric { get; set; }
        public ICollection<CriterionEvaluation> CriterionEvaluations { get; set; } = new List<CriterionEvaluation>();
    }
}
