using System;
using System.ComponentModel.DataAnnotations;
using StudyGo.Enums;

namespace StudyGo.ViewModels
{
    public class QuizViewModel : ActivityViewModel
    {
        [Required(ErrorMessage = "El modo de selección es obligatorio.")]
        [Display(Name = "Modo de selección")]
        public SelectionMode SelectionMode { get; set; }
    }
}
