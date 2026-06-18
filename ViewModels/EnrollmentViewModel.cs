using System;
using System.ComponentModel.DataAnnotations;
using StudyGo.Enums;

namespace StudyGo.ViewModels
{
    public class EnrollmentViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El estudiante es obligatorio.")]
        [Display(Name = "Estudiante")]
        public Guid StudentId { get; set; }

        [Required(ErrorMessage = "El curso es obligatorio.")]
        [Display(Name = "Curso")]
        public Guid CourseId { get; set; }

        [Required(ErrorMessage = "El usuario que inscribe es obligatorio.")]
        [Display(Name = "Inscrito por")]
        public Guid EnrolledBy { get; set; }

        [Required(ErrorMessage = "La fecha de inscripción es obligatoria.")]
        [Display(Name = "Fecha de inscripción")]
        public DateTime EnrolledAt { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio.")]
        [Display(Name = "Estado")]
        public EnrollmentStatus Status { get; set; }
    }
}
