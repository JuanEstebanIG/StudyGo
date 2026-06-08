using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class CalendarEventViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El curso es obligatorio.")]
        [Display(Name = "Curso")]
        public Guid CourseId { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "El título debe tener entre 3 y 200 caracteres.")]
        [Display(Name = "Título")]
        public string Title { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        [Display(Name = "Inicio")]
        public DateTime StartsAt { get; set; }

        [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
        [Display(Name = "Fin")]
        public DateTime EndsAt { get; set; }
    }
}