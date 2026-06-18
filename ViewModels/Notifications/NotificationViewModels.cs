// ============================================================================
// StudyGo · ViewModels/Notifications — módulo Jaison (Comunicación)
// ============================================================================
using System;
using System.Collections.Generic;

namespace StudyGo.ViewModels.Notifications
{
    public class NotificationItemViewModel
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public string? Link { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Icono FA6 según el tipo de notificación.</summary>
        public string Icon => Type switch
        {
            "NuevoMensaje"      => "fa-regular fa-comment-dots",
            "TareaEntregada"    => "fa-solid fa-code",
            "NuevoQuiz"         => "fa-solid fa-list-check",
            "CalificacionLista" => "fa-solid fa-star",
            "NuevoCurso"        => "fa-solid fa-layer-group",
            "Recordatorio"      => "fa-regular fa-clock",
            _                   => "fa-regular fa-bell",
        };

        /// <summary>Color del icono según el tipo.</summary>
        public string Color => Type switch
        {
            "NuevoMensaje"      => "text-brand-blue",
            "TareaEntregada"    => "text-brand-mint",
            "NuevoQuiz"         => "text-cyan-400",
            "CalificacionLista" => "text-yellow-400",
            "NuevoCurso"        => "text-brand-purple",
            "Recordatorio"      => "text-orange-400",
            _                   => "text-dark-muted",
        };

        public string TimeAgo
        {
            get
            {
                var diff = DateTime.UtcNow - CreatedAt;
                if (diff.TotalMinutes < 1)  return "ahora";
                if (diff.TotalMinutes < 60) return $"hace {(int)diff.TotalMinutes} min";
                if (diff.TotalHours   < 24) return $"hace {(int)diff.TotalHours} h";
                if (diff.TotalDays    <  7) return $"hace {(int)diff.TotalDays} d";
                return CreatedAt.ToLocalTime().ToString("dd/MM/yyyy");
            }
        }
    }

    public class NotificationDropdownViewModel
    {
        public List<NotificationItemViewModel> Items { get; set; } = new();
        public int UnreadCount { get; set; }
        public bool HasMore { get; set; }
    }
}
