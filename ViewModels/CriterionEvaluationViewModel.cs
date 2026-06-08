using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class CriterionEvaluationViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "La calificación es obligatoria.")]
        [Display(Name = "Calificación")]
        public Guid GradeId { get; set; }

        [Required(ErrorMessage = "El criterio de rúbrica es obligatorio.")]
        [Display(Name = "Criterio")]
        public Guid RubricCriteriaId { get; set; }

        [Required(ErrorMessage = "El puntaje es obligatorio.")]
        [Range(0, 100, ErrorMessage = "El puntaje debe estar entre 0 y 100.")]
        [Display(Name = "Puntaje")]
        public decimal Score { get; set; }

        [StringLength(1000, ErrorMessage = "El comentario no puede superar los 1000 caracteres.")]
        [Display(Name = "Comentario")]
        public string Comment { get; set; }
    }
}