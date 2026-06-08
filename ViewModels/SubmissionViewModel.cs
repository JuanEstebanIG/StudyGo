using System;
using System.ComponentModel.DataAnnotations;
using StudyGo.Enums;

namespace StudyGo.ViewModels
{
    public class SubmissionViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "La tarea es obligatoria.")]
        [Display(Name = "Tarea de programación")]
        public Guid ProgrammingTaskId { get; set; }

        [Required(ErrorMessage = "El estudiante es obligatorio.")]
        [Display(Name = "Estudiante")]
        public Guid StudentId { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio.")]
        [Display(Name = "Estado")]
        public SubmissionStatus Status { get; set; }
    }
}