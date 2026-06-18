using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class GradeViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "La entrega es obligatoria.")]
        [Display(Name = "Entrega")]
        public Guid SubmissionId { get; set; }

        [Required(ErrorMessage = "La nota final es obligatoria.")]
        [Range(0, 100, ErrorMessage = "La nota debe estar entre 0 y 100.")]
        [Display(Name = "Nota final")]
        public decimal FinalScore { get; set; }

        [Required(ErrorMessage = "La fecha de calificación es obligatoria.")]
        [Display(Name = "Calificado el")]
        public DateTime GradedAt { get; set; }
    }
}