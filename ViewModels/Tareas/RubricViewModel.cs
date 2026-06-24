using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class RubricViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "La tarea de programación es obligatoria.")]
        [Display(Name = "Tarea de programación")]
        public Guid ProgrammingTaskId { get; set; }
    }
}