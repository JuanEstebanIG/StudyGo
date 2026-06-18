using System;
using System.ComponentModel.DataAnnotations;
using StudyGo.Enums;

namespace StudyGo.ViewModels
{
    public class ChatViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "El tipo de chat es obligatorio.")]
        [Display(Name = "Tipo de chat")]
        public ChatType Type { get; set; }
    }
}