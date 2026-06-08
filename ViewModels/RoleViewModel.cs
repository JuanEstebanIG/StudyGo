using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class RoleViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        [Display(Name = "Nombre del rol")]
        public string Name { get; set; }
    }
}