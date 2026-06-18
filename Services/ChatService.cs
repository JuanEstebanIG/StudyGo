// ============================================================================
// StudyGo · Services/ChatService.cs — implementación EF Core
// ----------------------------------------------------------------------------
// Reglas aplicadas:
//  - Privacidad (§8/§12.1): solo se devuelven chats donde el usuario participa.
//    Como un docente nunca es participante de un chat privado entre estudiantes,
//    el filtro por participación ya impide que lo vea.
//  - Cifrado: el contenido se descifra vía IMessageCipher antes de mostrarlo.
//  - "No leídos": el modelo actual (ChatParticipant) no guarda LastReadAt, así
//    que UnreadCount queda en 0. Para soportarlo habría que añadir ese campo
//    (cambio de dominio → migración; decisión del dueño del dominio).
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using StudyGo.Enums;
using StudyGo.Models;
using StudyGo.ViewModels.Chat;

namespace StudyGo.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _db;
        private readonly IMessageCipher _cipher;

        public ChatService(AppDbContext db, IMessageCipher cipher)
        {
            _db = db;
            _cipher = cipher;
        }

        public async Task<bool> CanAccessAsync(Guid chatId, Guid userId) =>
            await _db.ChatParticipants.AnyAsync(p => p.ChatId == chatId && p.UserId == userId);

        public async Task<List<ChatConversationSummary>> GetConversationsAsync(Guid currentUserId)
        {
            // Ids de los chats donde participa el usuario (privacidad por participación).
            var chatIds = await _db.ChatParticipants
                .Where(p => p.UserId == currentUserId)
                .Select(p => p.ChatId)
                .ToListAsync();

            if (chatIds.Count == 0)
                return new List<ChatConversationSummary>();

            var chats = await _db.Chats
                .Where(c => chatIds.Contains(c.Id))
                .Include(c => c.Participants).ThenInclude(p => p.User)
                .ToListAsync();

            // Último mensaje por chat. Proyectamos lo mínimo y agrupamos en memoria
            // para evitar traducciones GroupBy->First() poco fiables en EF.
            var msgs = await _db.ChatMessages
                .Where(m => chatIds.Contains(m.ChatId))
                .Select(m => new { m.ChatId, m.SentAt, m.EncryptedContent })
                .ToListAsync();

            var lastLookup = msgs
                .GroupBy(x => x.ChatId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.SentAt).First());

            var list = chats.Select(c =>
            {
                lastLookup.TryGetValue(c.Id, out var last);
                return new ChatConversationSummary
                {
                    ChatId = c.Id,
                    Type = c.Type,
                    Title = ResolveTitle(c, currentUserId),
                    LastMessagePreview = last is null ? "" : Truncate(_cipher.Decrypt(last.EncryptedContent), 60),
                    LastMessageAt = last?.SentAt,
                    UnreadCount = 0, // TODO: requiere LastReadAt en ChatParticipant (migración)
                };
            })
            // Conversaciones con actividad primero, luego por nombre.
            .OrderByDescending(c => c.LastMessageAt ?? DateTime.MinValue)
            .ThenBy(c => c.Title)
            .ToList();

            return list;
        }

        public async Task<ChatThreadViewModel?> GetThreadAsync(Guid chatId, Guid currentUserId)
        {
            if (!await CanAccessAsync(chatId, currentUserId))
                return null; // no participa → sin acceso (privacidad)

            var chat = await _db.Chats
                .Include(c => c.Participants).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat is null) return null;

            var messages = await _db.ChatMessages
                .Where(m => m.ChatId == chatId)
                .Include(m => m.Sender)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return new ChatThreadViewModel
            {
                ChatId = chat.Id,
                Type = chat.Type,
                Title = ResolveTitle(chat, currentUserId),
                Participants = chat.Participants
                    .Select(p => p.User?.DisplayName ?? "Usuario")
                    .ToList(),
                Messages = messages.Select(m => ToItem(m, currentUserId)).ToList(),
            };
        }

        public async Task<ChatMessageItem> AddMessageAsync(Guid chatId, Guid senderId, string content)
        {
            var entity = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = senderId,
                EncryptedContent = _cipher.Encrypt(content?.Trim() ?? string.Empty),
                SentAt = DateTime.UtcNow,
            };

            _db.ChatMessages.Add(entity);
            await _db.SaveChangesAsync();

            entity.Sender = await _db.Users.FirstOrDefaultAsync(u => u.Id == senderId);
            return ToItem(entity, senderId);
        }

        // ----------------------------------------------------------------- helpers
        private ChatMessageItem ToItem(ChatMessage m, Guid currentUserId) => new()
        {
            Id = m.Id,
            SenderId = m.SenderId,
            SenderName = m.Sender?.DisplayName ?? "Usuario",
            Content = _cipher.Decrypt(m.EncryptedContent),
            SentAt = m.SentAt,
            IsOwn = m.SenderId == currentUserId,
            Status = "sent",
        };

        /// <summary>Privado: nombre del otro. Grupal: nombres unidos (no hay campo de nombre de grupo).</summary>
        private static string ResolveTitle(Chat chat, Guid currentUserId)
        {
            var others = chat.Participants
                .Where(p => p.UserId != currentUserId)
                .Select(p => p.User?.DisplayName ?? "Usuario")
                .ToList();

            if (chat.Type == ChatType.Privado)
                return others.FirstOrDefault() ?? "Conversación";

            if (others.Count == 0) return "Chat grupal";
            if (others.Count <= 2) return string.Join(", ", others);
            return $"{string.Join(", ", others.Take(2))} y {others.Count - 2} más";
        }

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max).TrimEnd() + "…");
    }
}
