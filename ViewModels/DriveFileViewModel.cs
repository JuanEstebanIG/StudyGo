using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class DriveFileViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El propietario es obligatorio.")]
        [Display(Name = "Propietario")]
        public Guid OwnerId { get; set; }

        [Required(ErrorMessage = "El curso es obligatorio.")]
        [Display(Name = "Curso")]
        public Guid CourseId { get; set; }

        [Required(ErrorMessage = "El ID del archivo en Drive es obligatorio.")]
        [StringLength(300, MinimumLength = 1, ErrorMessage = "El DriveFileId debe tener entre 1 y 300 caracteres.")]
        [Display(Name = "ID en Drive")]
        public string DriveFileId { get; set; }

        [Required(ErrorMessage = "La URL es obligatoria.")]
        [Url(ErrorMessage = "La URL no tiene un formato válido.")]
        [StringLength(2000, ErrorMessage = "La URL no puede superar los 2000 caracteres.")]
        [Display(Name = "URL")]
        public string Url { get; set; }
    }
}
