using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class UserViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "La institución es obligatoria.")]
        [Display(Name = "Institución")]
        public Guid InstitutionId { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        [StringLength(256, ErrorMessage = "El correo no puede superar los 256 caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre de usuario debe tener entre 2 y 100 caracteres.")]
        [Display(Name = "Nombre de usuario")]
        public string DisplayName { get; set; }
    }
}