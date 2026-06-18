// ============================================================================
// StudyGo · Hubs/ChatHub.cs — tiempo real del chat (§12.1)
// ----------------------------------------------------------------------------
// Un grupo de SignalR por cada chatId. El cliente (chat.js):
//   - llama JoinChat(chatId) al abrir una conversación,
//   - SendMessage(chatId, content) para enviar,
//   - Typing(chatId) mientras escribe.
// El servidor reemite:
//   - ReceiveMessage(item)  a todo el grupo,
//   - UserTyping(senderName) a los demás del grupo.
// El control de acceso (privacidad §8) se valida en el servidor vía IChatService.
// ============================================================================
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using StudyGo.Services;

namespace StudyGo.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chat;
        private readonly ICurrentUserResolver _currentUser;

        public ChatHub(IChatService chat, ICurrentUserResolver currentUser)
        {
            _chat = chat;
            _currentUser = currentUser;
        }

        private static string Group(Guid chatId) => $"chat:{chatId}";

        public async Task JoinChat(Guid chatId)
        {
            var me = await _currentUser.ResolveAsync(Context.User);
            if (me is null || !await _chat.CanAccessAsync(chatId, me.Id))
                throw new HubException("No tienes acceso a esta conversación.");

            await Groups.AddToGroupAsync(Context.ConnectionId, Group(chatId));
        }

        public async Task LeaveChat(Guid chatId) =>
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(chatId));

        public async Task SendMessage(Guid chatId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            var me = await _currentUser.ResolveAsync(Context.User);
            if (me is null || !await _chat.CanAccessAsync(chatId, me.Id))
                throw new HubException("No tienes acceso a esta conversación.");

            var item = await _chat.AddMessageAsync(chatId, me.Id, content);

            // Se reemite a todos (cada cliente decide si es propio por SenderId).
            await Clients.Group(Group(chatId)).SendAsync("ReceiveMessage", new
            {
                id = item.Id,
                chatId,
                senderId = item.SenderId,
                senderName = item.SenderName,
                content = item.Content,
                sentAt = item.SentAt,
            });
        }

        public async Task Typing(Guid chatId)
        {
            var me = await _currentUser.ResolveAsync(Context.User);
            if (me is null) return;
            await Clients.OthersInGroup(Group(chatId)).SendAsync("UserTyping", me.DisplayName);
        }
    }
}
