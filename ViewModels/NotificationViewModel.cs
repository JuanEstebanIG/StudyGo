using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class NotificationViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio.")]
        [Display(Name = "Usuario")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "El tipo de notificación es obligatorio.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "El tipo debe tener entre 1 y 100 caracteres.")]
        [Display(Name = "Tipo")]
        public string Type { get; set; }

        [Display(Name = "Leída")]
        public bool IsRead { get; set; }

        [Required(ErrorMessage = "La fecha de creación es obligatoria.")]
        [Display(Name = "Creada el")]
        public DateTime CreatedAt { get; set; }
    }
}