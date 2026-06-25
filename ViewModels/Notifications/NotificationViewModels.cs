// ============================================================================
// StudyGo · ViewModels/Notifications/NotificationViewModels.cs
// ============================================================================
using System;
using System.Collections.Generic;

namespace StudyGo.ViewModels.Notifications
{
    public class NotificationListViewModel
    {
        public int UnreadCount { get; set; }
        public List<NotificationItemViewModel> Notifications { get; set; } = new();
    }

    public class NotificationItemViewModel
    {
        public Guid Id { get; set; } // Tipo Guid mapeado correctamente
        public string Type { get; set; }
        public string Message { get; set; }
        public string Link { get; set; }
        public string TimeRelative { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } // Requerido por la línea 97 del servicio
    }

    public class NotificationDropdownViewModel
    {
        public int UnreadCount { get; set; }
        public List<NotificationItemViewModel> Items { get; set; } = new(); // Cambiado de 'Data' a 'Items' para la línea 47
        public bool HasMore { get; set; } // Requerido por la línea 49 del servicio
    }
}