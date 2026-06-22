using System;
using System.Collections.Generic;

namespace StudyGo.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de ejecución del quiz (estudiante rindiendo el examen).
    /// Contiene toda la información necesaria para renderizar las preguntas y el temporizador.
    /// </summary>
    public class TakeQuizViewModel
    {
        public Guid QuizId { get; set; }
        public Guid AttemptId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CourseName { get; set; }
        public int TimeLimitMinutes { get; set; }
        public DateTime StartedAt { get; set; }
        public int TotalQuestions { get; set; }

        /// <summary>
        /// Preguntas del quiz con sus opciones (sin marcar cuáles son correctas).
        /// </summary>
        public List<TakeQuizQuestion> Questions { get; set; } = new();
    }

    /// <summary>
    /// Pregunta individual para la vista de ejecución.
    /// </summary>
    public class TakeQuizQuestion
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public int Order { get; set; }
        public List<TakeQuizOption> Options { get; set; } = new();
    }

    /// <summary>
    /// Opción de respuesta para la vista de ejecución (NO incluye IsCorrect).
    /// </summary>
    public class TakeQuizOption
    {
        public Guid OptionId { get; set; }
        public string OptionText { get; set; }
    }
}
