using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class CourseViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "La institución es obligatoria.")]
        [Display(Name = "Institución")]
        public Guid InstitutionId { get; set; }

        [Required(ErrorMessage = "El docente es obligatorio.")]
        [Display(Name = "Docente")]
        public Guid TeacherId { get; set; }

        [Required(ErrorMessage = "El nombre del curso es obligatorio.")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 200 caracteres.")]
        [Display(Name = "Nombre del curso")]
        public string Name { get; set; }
    }
}