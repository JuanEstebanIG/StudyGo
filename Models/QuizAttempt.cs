using System;

namespace StudyGo.Models
{
    public class QuizAttempt
    {
        public Guid Id { get; set; }
        public Guid QuizId { get; set; }
        public Guid StudentId { get; set; }
        public decimal Score { get; set; }
        public DateTime SubmittedAt { get; set; }

        public Quiz Quiz { get; set; }
        public User Student { get; set; }
    }
}