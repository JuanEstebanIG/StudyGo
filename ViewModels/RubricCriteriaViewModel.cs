using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class RubricCriteriaViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "La rúbrica es obligatoria.")]
        [Display(Name = "Rúbrica")]
        public Guid RubricId { get; set; }

        [Required(ErrorMessage = "La descripción del criterio es obligatoria.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "La descripción debe tener entre 5 y 500 caracteres.")]
        [Display(Name = "Descripción")]
        public string Description { get; set; }

        [Required(ErrorMessage = "El peso es obligatorio.")]
        [Range(0.01, 100, ErrorMessage = "El peso debe estar entre 0.01 y 100.")]
        [Display(Name = "Peso (%)")]
        public decimal Weight { get; set; }
    }
}