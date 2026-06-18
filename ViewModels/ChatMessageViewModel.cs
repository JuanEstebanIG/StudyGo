using System;
using System.ComponentModel.DataAnnotations;

namespace StudyGo.ViewModels
{
    public class ChatMessageViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El chat es obligatorio.")]
        [Display(Name = "Chat")]
        public Guid ChatId { get; set; }

        [Required(ErrorMessage = "El remitente es obligatorio.")]
        [Display(Name = "Remitente")]
        public Guid SenderId { get; set; }

        [Required(ErrorMessage = "El contenido del mensaje es obligatorio.")]
        [StringLength(4000, MinimumLength = 1, ErrorMessage = "El mensaje debe tener entre 1 y 4000 caracteres.")]
        [Display(Name = "Contenido")]
        public string EncryptedContent { get; set; }

        [Required(ErrorMessage = "La fecha de envío es obligatoria.")]
        [Display(Name = "Enviado el")]
        public DateTime SentAt { get; set; }
    }
}