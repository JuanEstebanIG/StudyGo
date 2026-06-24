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
        Task<List<ChatConversationSummary>> GetConversationsAsync(Guid currentUserId);
        Task<ChatThreadViewModel?> GetThreadAsync(Guid chatId, Guid currentUserId);
        Task<bool> CanAccessAsync(Guid chatId, Guid userId);
        Task<ChatMessageItem> AddMessageAsync(Guid chatId, Guid senderId, string content);

        // AÑADIDO: Contrato dinámico para abrir/recuperar chats privados desde cero
        Task<Guid> GetOrCreatePrivateChatAsync(Guid userId1, Guid userId2);

        Task<List<StudyGo.Models.User>> SearchUsersByEmailAsync(string emailQuery, Guid currentUserId);
    }
}
