using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class InstitutionViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El nombre de la institución es obligatorio.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 200 caracteres.")]
        [Display(Name = "Nombre")]
        public string Name { get; set; }
    }
}
