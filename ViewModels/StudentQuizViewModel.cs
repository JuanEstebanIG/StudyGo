using System;
using System.Collections.Generic;

namespace StudyGo.ViewModels
{
    /// <summary>
    /// ViewModel para la vista principal de evaluaciones del Estudiante.
    /// Contiene quizzes activos disponibles y el historial de intentos.
    /// </summary>
    public class StudentQuizViewModel
    {
        public List<StudentQuizCard> ActiveQuizzes { get; set; } = new();
        public List<StudentAttemptHistory> AttemptHistory { get; set; } = new();
    }

    /// <summary>
    /// Tarjeta de un quiz activo/disponible para el estudiante.
    /// </summary>
    public class StudentQuizCard
    {
        public Guid QuizId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CourseName { get; set; }
        public int TimeLimitMinutes { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? OpenDate { get; set; }
        public int MaxAttempts { get; set; }
        public int AttemptsUsed { get; set; }
        public int QuestionCount { get; set; }
        public bool HasCompletedAttempt { get; set; }

        /// <summary>
        /// Intentos restantes calculados dinámicamente.
        /// </summary>
        public int RemainingAttempts => MaxAttempts - AttemptsUsed;
    }

    /// <summary>
    /// Registro del historial de intentos del estudiante.
    /// </summary>
    public class StudentAttemptHistory
    {
        public Guid AttemptId { get; set; }
        public Guid QuizId { get; set; }
        public string QuizTitle { get; set; }
        public string CourseName { get; set; }
        public decimal Score { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Tiempo real utilizado por el estudiante formateado.
        /// </summary>
        public string TimeSpentDisplay
        {
            get
            {
                var diff = SubmittedAt - StartedAt;
                return $"{(int)diff.TotalMinutes}:{diff.Seconds:D2}";
            }
        }
    }
}
