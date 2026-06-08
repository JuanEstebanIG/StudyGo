using System;
using System.Collections.Generic;

namespace StudyGo.Models
{
    public class Rubric
    {
        public Guid Id { get; set; }
        public Guid ProgrammingTaskId { get; set; }

        public ProgrammingTask ProgrammingTask { get; set; }
        public ICollection<RubricCriteria> Criteria { get; set; } = new List<RubricCriteria>();
    }
}
