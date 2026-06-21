using System;
using System.Collections.Generic;
using StudyGo.Enums;

namespace StudyGo.Models
{
    public class Quiz : Activity
    {
        public SelectionMode SelectionMode { get; set; }

        /// <summary>
        /// Fecha límite de entrega del quiz.
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Duración máxima del examen en minutos.
        /// </summary>
        public int TimeLimitMinutes { get; set; }

        /// <summary>
        /// Fecha y hora de apertura del quiz.
        /// </summary>
        public DateTime? OpenDate { get; set; }

        /// <summary>
        /// Número máximo de intentos permitidos por estudiante.
        /// </summary>
        public int MaxAttempts { get; set; } = 1;

        public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
        public List<QuizQuestion> Questions { get; set; } = new();
    }
}
