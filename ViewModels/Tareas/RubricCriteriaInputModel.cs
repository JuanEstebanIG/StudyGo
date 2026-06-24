using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels.Tareas
{
    public class RubricCriteriaInputModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "La descripción debe tener entre 3 y 500 caracteres.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "El peso es obligatorio.")]
        [Range(0.01, 100, ErrorMessage = "El peso debe estar entre 0.01 y 100.")]
        public decimal Weight { get; set; }
    }
}