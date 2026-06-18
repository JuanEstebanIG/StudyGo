// ============================================================================
// StudyGo · Services/IChatService.cs — caso de uso del módulo Comunicación
// El controlador y el hub dependen de esta interfaz; la vista nunca toca EF.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StudyGo.ViewModels.Chat;

namespace StudyGo.Services
{
    public interface IChatService
    {
        /// <summary>Conversaciones del usuario (solo en las que participa → §8 privacidad).</summary>
        Task<List<ChatConversationSummary>> GetConversationsAsync(Guid currentUserId);

        /// <summary>Hilo de una conversación. Devuelve null si el usuario no participa.</summary>
        Task<ChatThreadViewModel?> GetThreadAsync(Guid chatId, Guid currentUserId);

        /// <summary>¿El usuario participa en el chat? (control de acceso del hub/endpoint).</summary>
        Task<bool> CanAccessAsync(Guid chatId, Guid userId);

        /// <summary>Persiste un mensaje y devuelve el item listo para pintar.</summary>
        Task<ChatMessageItem> AddMessageAsync(Guid chatId, Guid senderId, string content);
    }
}
