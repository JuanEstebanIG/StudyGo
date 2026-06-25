// ============================================================================
// StudyGo · ViewModels/Chat — módulo de Jaison (Comunicación)
// ViewModels de PRESENTACIÓN del chat. El controlador los arma a partir del
// servicio de aplicación (IChatService); las vistas nunca tocan EF (§2).
// ============================================================================
using System;
using System.Collections.Generic;
using StudyGo.Enums;

namespace StudyGo.ViewModels.Chat
{
    /// <summary>Modelo de la pantalla de chat (lista + hilo activo).</summary>
    public class ChatPageViewModel
    {
        public Guid CurrentUserId { get; set; }

        /// <summary>True si el usuario es Docente/Admin (afecta privacidad, §8/§12.1).</summary>
        public bool IsTeacher { get; set; }

        public List<ChatConversationSummary> Conversations { get; set; } = new();

        /// <summary>Hilo abierto al cargar la página; null = ninguno seleccionado.</summary>
        public ChatThreadViewModel? ActiveConversation { get; set; }

        public bool HasConversations => Conversations.Count > 0;
    }

    /// <summary>Item de la lista lateral de conversaciones.</summary>
    public class ChatConversationSummary
    {
        public Guid ChatId { get; set; }
        public ChatType Type { get; set; }

        /// <summary>Nombre a mostrar (otro participante en privados; nombre del grupo en grupales).</summary>
        public string Title { get; set; } = "";

        public string? AvatarUrl { get; set; }
        public string LastMessagePreview { get; set; } = "";
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public bool IsActive { get; set; }
        public string TargetRole { get; set; } = "Estudiante";
    }

    /// <summary>Hilo de conversación abierto a la derecha.</summary>
    public class ChatThreadViewModel
    {
        public Guid ChatId { get; set; }
        public string Title { get; set; } = "";
        public ChatType Type { get; set; }
        public bool IsPrivate => Type == ChatType.Privado;

        /// <summary>Nombres de los participantes (para el header del hilo).</summary>
        public List<string> Participants { get; set; } = new();

        public List<ChatMessageItem> Messages { get; set; } = new();
    }

    /// <summary>Una burbuja de mensaje.</summary>
    public class ChatMessageItem
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = "";
        public string? SenderAvatarUrl { get; set; }

        /// <summary>Contenido ya descifrado y listo para mostrar (ver IMessageCipher).</summary>
        public string Content { get; set; } = "";

        public DateTime SentAt { get; set; }

        /// <summary>True si lo envió el usuario actual (burbuja a la derecha).</summary>
        public bool IsOwn { get; set; }

        /// <summary>sending | sent (estado de envío, §12.1).</summary>
        public string Status { get; set; } = "sent";
        public bool IsDeleted { get; set; }
    }

    /// <summary>Payload para enviar un mensaje (POST de respaldo si SignalR cae).</summary>
    public class SendMessageInput
    {
        public Guid ChatId { get; set; }
        public string Content { get; set; } = "";
    }
}
