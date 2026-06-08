using System;
using System.Collections.Generic;
using StudyGo.Enums;

namespace StudyGo.Models
{
    public class Quiz : Activity
    {
        public SelectionMode SelectionMode { get; set; }

        public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    }
}
