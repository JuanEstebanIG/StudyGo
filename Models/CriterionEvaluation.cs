using System;

namespace StudyGo.Models
{
    public class CriterionEvaluation
    {
        public Guid Id { get; set; }
        public Guid GradeId { get; set; }
        public Guid RubricCriteriaId { get; set; }
        public decimal Score { get; set; }
        public string Comment { get; set; }

        public Grade Grade { get; set; }
        public RubricCriteria RubricCriteria { get; set; }
    }
}