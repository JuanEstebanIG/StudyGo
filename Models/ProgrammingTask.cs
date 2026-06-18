using System;
using System.Collections.Generic;

namespace StudyGo.Models
{
    public class ProgrammingTask : Activity
    {
        public string Language { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int MemoryLimitMb { get; set; }

        public Rubric Rubric { get; set; }
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }
}