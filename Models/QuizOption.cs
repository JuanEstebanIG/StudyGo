using System;

namespace StudyGo.Models
{
    /// <summary>
    /// Representa una opción de respuesta dentro de una QuizQuestion.
    /// El campo IsCorrect indica si esta opción es correcta (para autocalificación).
    /// </summary>
    public class QuizOption
    {
        public Guid Id { get; set; }
        public Guid QuizQuestionId { get; set; }
        public string OptionText { get; set; }

        /// <summary>
        /// Indica si esta opción es la respuesta correcta (o una de las correctas en selección múltiple).
        /// </summary>
        public bool IsCorrect { get; set; }

        // Navegación inversa hacia la pregunta padre
        public QuizQuestion QuizQuestion { get; set; }
    }
}
