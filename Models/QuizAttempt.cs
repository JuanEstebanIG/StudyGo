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

        /// <summary>
        /// Momento exacto en que el estudiante inició el intento del quiz.
        /// Se usa para calcular el tiempo real empleado y validar el límite de tiempo.
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// JSON serializado con las respuestas seleccionadas por el estudiante.
        /// Formato: { "questionId": ["optionId1", "optionId2"], ... }
        /// </summary>
        public string AnswersJson { get; set; }

        public Quiz Quiz { get; set; }
        public User Student { get; set; }
    }
}