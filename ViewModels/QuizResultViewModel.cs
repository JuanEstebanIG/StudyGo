using System;
using System.Collections.Generic;

namespace StudyGo.ViewModels
{
    /// <summary>
    /// ViewModel para mostrar el resultado inmediato del quiz después de enviarlo.
    /// </summary>
    public class QuizResultViewModel
    {
        public Guid AttemptId { get; set; }
        public Guid QuizId { get; set; }
        public string QuizTitle { get; set; }
        public string CourseName { get; set; }
        public decimal Score { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime StartedAt { get; set; }
        public int TimeLimitMinutes { get; set; }

        /// <summary>
        /// Tiempo utilizado por el estudiante formateado.
        /// </summary>
        public string TimeSpentDisplay
        {
            get
            {
                var diff = SubmittedAt - StartedAt;
                return $"{(int)diff.TotalMinutes} min {diff.Seconds} seg";
            }
        }

        /// <summary>
        /// Porcentaje de aciertos calculado.
        /// </summary>
        public decimal Percentage => TotalQuestions > 0 ? Math.Round((decimal)CorrectAnswers / TotalQuestions * 100, 1) : 0;

        /// <summary>
        /// Detalle por pregunta para la revisión.
        /// </summary>
        public List<QuestionResultDetail> Details { get; set; } = new();
    }

    /// <summary>
    /// Detalle del resultado de una pregunta individual.
    /// </summary>
    public class QuestionResultDetail
    {
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public bool IsCorrect { get; set; }
        public List<string> SelectedOptions { get; set; } = new();
        public List<string> CorrectOptions { get; set; } = new();
    }
}
