using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels.Tareas
{
    public class CriterionEvaluationInputModel
    {
        public Guid RubricCriteriaId { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "La puntuación debe estar entre 0 y 100.")]
        public decimal Score { get; set; }

        [StringLength(1000, ErrorMessage = "El comentario no puede exceder 1000 caracteres.")]
        public string Comment { get; set; }
    }
}