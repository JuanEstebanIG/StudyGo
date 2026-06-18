using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class SubmissionVersionViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "La entrega es obligatoria.")]
        [Display(Name = "Entrega")]
        public Guid SubmissionId { get; set; }

        [Required(ErrorMessage = "El número de versión es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El número de versión debe ser mayor a 0.")]
        [Display(Name = "Versión")]
        public int VersionNumber { get; set; }

        [Required(ErrorMessage = "El código es obligatorio.")]
        [MinLength(1, ErrorMessage = "El código no puede estar vacío.")]
        [Display(Name = "Código fuente")]
        public string Code { get; set; }

        [Required(ErrorMessage = "La fecha de guardado es obligatoria.")]
        [Display(Name = "Guardado el")]
        public DateTime SavedAt { get; set; }
    }
}