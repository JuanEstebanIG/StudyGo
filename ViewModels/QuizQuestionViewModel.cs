using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    /// <summary>
    /// ViewModel para una pregunta del quiz en el formulario de creación/edición.
    /// </summary>
    public class QuizQuestionViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "El texto de la pregunta es obligatorio.")]
        [Display(Name = "Pregunta")]
        public string QuestionText { get; set; }

        [Required(ErrorMessage = "El tipo de pregunta es obligatorio.")]
        [Display(Name = "Tipo de respuesta")]
        public string QuestionType { get; set; } = "Unica";

        public int Order { get; set; }

        public List<QuizOptionViewModel> Options { get; set; } = new();
    }
}
