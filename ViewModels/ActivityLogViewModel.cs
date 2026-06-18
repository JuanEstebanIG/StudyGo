using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class ActivityLogViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [Display(Name = "Usuario")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "La acción es obligatoria.")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "La acción debe tener entre 1 y 500 caracteres.")]
        [Display(Name = "Acción")]
        public string Action { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        [Display(Name = "Fecha")]
        public DateTime Timestamp { get; set; }
    }
}