using System;
using System.Collections.Generic;

namespace StudyGo.Models
{
    /// <summary>
    /// Representa una pregunta individual dentro de un Quiz.
    /// Cada pregunta define su propio tipo de selección (Única o Múltiple).
    /// </summary>
    public class QuizQuestion
    {
        public Guid Id { get; set; }
        public Guid QuizId { get; set; }
        public string QuestionText { get; set; }

        /// <summary>
        /// Tipo de respuesta: "Unica" o "Multiple".
        /// Define si el estudiante selecciona una sola opción o varias.
        /// </summary>
        public string QuestionType { get; set; } = "Unica";

        /// <summary>
        /// Orden de aparición de la pregunta dentro del quiz.
        /// </summary>
        public int Order { get; set; }

        // Navegación inversa hacia el Quiz padre
        public Quiz Quiz { get; set; }

        // Navegación hacia las opciones de respuesta
        public List<QuizOption> Options { get; set; } = new();
    }
}
