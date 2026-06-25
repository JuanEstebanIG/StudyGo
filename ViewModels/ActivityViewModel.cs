using System;
using System.ComponentModel.DataAnnotations;
using StudyGo.Enums;

namespace StudyGo.ViewModels
{
    public abstract class ActivityViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El curso es obligatorio.")]
        [Display(Name = "Curso")]
        public Guid CourseId { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "El título debe tener entre 3 y 200 caracteres.")]
        [Display(Name = "Título")]
        public string Title { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [MinLength(10, ErrorMessage = "La descripción debe tener al menos 10 caracteres.")]
        [Display(Name = "Descripción")]
        public string Description { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio.")]
        [Display(Name = "Estado")]
        public ActivityState State { get; set; }

        // Nueva propiedad con soporte para Fecha y Hora
        [Display(Name = "Fecha y Hora de Entrega")]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7); // Por defecto una semana en el futuro
    }
}
