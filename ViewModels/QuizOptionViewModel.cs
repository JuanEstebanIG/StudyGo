using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    /// <summary>
    /// ViewModel para una opción de respuesta en el formulario de creación/edición.
    /// </summary>
    public class QuizOptionViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "El texto de la opción es obligatorio.")]
        [Display(Name = "Opción")]
        public string OptionText { get; set; }

        [Display(Name = "Es correcta")]
        public bool IsCorrect { get; set; }
    }
}
