using System;

namespace StudyGo.Models
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Type { get; set; } = "";

        /// <summary>Texto descriptivo de la notificación (añadido en migración v2).</summary>
        public string Message { get; set; } = "";

        /// <summary>URL opcional a la que navegar al hacer clic.</summary>
        public string? Link { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
