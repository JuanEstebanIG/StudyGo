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
            // 1. Traemos los IDs de los chats donde participa el usuario
            var chatIds = await _db.ChatParticipants
                .Where(p => p.UserId == currentUserId)
                .Select(p => p.ChatId)
                .ToListAsync();

            if (chatIds.Count == 0)
                return new List<ChatConversationSummary>();

            // 2. Cargamos los chats incluyendo explícitamente los participantes y sus datos de usuario
            var chats = await _db.Chats
                .Where(c => chatIds.Contains(c.Id))
                .Include(c => c.Participants)
                    .ThenInclude(p => p.User) // Fuerza a SQL Server a traer la tabla de usuarios vinculada
                .ToListAsync();

            // 3. Obtenemos el último mensaje de cada uno de esos chats
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
                    // Si no hay mensajes, dejamos un texto amigable estilo WhatsApp
                    LastMessagePreview = last is null ? "Conversación nueva (sin mensajes)" : Truncate(_cipher.Decrypt(last.EncryptedContent), 60),
                    // Si es null, usamos DateTime.MinValue para el ordenamiento en memoria
                    LastMessageAt = last?.SentAt,
                    UnreadCount = 0
                };
            })
            // Forzamos a que las conversaciones nuevas (sin mensajes) aparezcan arriba o se ordenen por título si no tienen actividad
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

            entity.Sender = (await _db.Users.FirstOrDefaultAsync(u => u.Id == senderId))!;
            return ToItem(entity, senderId);
        }

        /// <summary>Busca un chat privado existente entre dos usuarios o crea uno nuevo desde cero.</summary>
        public async Task<Guid> GetOrCreatePrivateChatAsync(Guid userId1, Guid userId2)
        {
            var existingChatId = await _db.Chats
                .Where(c => c.Type == ChatType.Privado)
                .Where(c =>
                    (c.Participants.Any(p => p.UserId == userId1) || _db.ChatMessages.Any(m => m.ChatId == c.Id && m.SenderId == userId1))
                    &&
                    (c.Participants.Any(p => p.UserId == userId2) || _db.ChatMessages.Any(m => m.ChatId == c.Id && m.SenderId == userId2))
                )
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (existingChatId != Guid.Empty)
            {
                var currentParticipants = await _db.ChatParticipants
                    .Where(p => p.ChatId == existingChatId)
                    .Select(p => p.UserId)
                    .ToListAsync();

                // NUEVO: Bandera para saber si el usuario que inicia la petición estaba por fuera (reingresando)
                bool user1Reentering = !currentParticipants.Contains(userId1);

                if (user1Reentering)
                {
                    _db.ChatParticipants.Add(new ChatParticipant { ChatId = existingChatId, UserId = userId1 });

                    // Buscamos el nombre del usuario que está volviendo para armar la alerta
                    var user1Entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId1);
                    string user1Name = user1Entity?.DisplayName ?? "Un usuario";

                    // Creamos e inyectamos el mensaje de reingreso en el historial
                    var reentryMessage = new ChatMessage
                    {
                        Id = Guid.NewGuid(),
                        ChatId = existingChatId,
                        SenderId = userId1,
                        EncryptedContent = _cipher.Encrypt($"📢 {user1Name} se ha reincorporado a la conversación."),
                        SentAt = DateTime.UtcNow
                    };
                    _db.ChatMessages.Add(reentryMessage);
                }

                if (!currentParticipants.Contains(userId2))
                {
                    _db.ChatParticipants.Add(new ChatParticipant { ChatId = existingChatId, UserId = userId2 });
                }

                await _db.SaveChangesAsync();
                return existingChatId;
            }

            var newChat = new Chat { Id = Guid.NewGuid(), Type = ChatType.Privado };
            _db.Chats.Add(newChat);

            _db.ChatParticipants.Add(new ChatParticipant { ChatId = newChat.Id, UserId = userId1 });
            _db.ChatParticipants.Add(new ChatParticipant { ChatId = newChat.Id, UserId = userId2 });

            await _db.SaveChangesAsync();
            return newChat.Id;
        }

        // ----------------------------------------------------------------- helpers
        private ChatMessageItem ToItem(ChatMessage m, Guid currentUserId) => new()
        {
            Id = m.Id,
            SenderId = m.SenderId,
            SenderName = m.Sender?.DisplayName ?? "Usuario",
            // Si está eliminado, sobreescribimos el contenido y no desciframos nada.
            Content = m.IsDeleted ? "🚫 Este mensaje fue eliminado..." : _cipher.Decrypt(m.EncryptedContent),
            SentAt = m.SentAt,
            IsOwn = m.SenderId == currentUserId,
            Status = m.IsDeleted ? "deleted" : "sent",
            IsDeleted = m.IsDeleted // Pasamos el flag al frontend
        };

        /// <summary>Privado: nombre del otro. Grupal: nombres unidos (no hay campo de nombre de grupo).</summary>
        private string ResolveTitle(Chat chat, Guid currentUserId)
        {
            // 1. Si el chat tiene un nombre explícito (Grupos), lo usamos directamente
            if (!string.IsNullOrWhiteSpace(chat.Name))
                return chat.Name;

            // 2. Si es privado, recuperamos históricamente a los involucrados
            if (chat.Type == ChatType.Privado)
            {
                // Intentamos primero con los participantes activos
                var activeOther = chat.Participants.FirstOrDefault(p => p.UserId != currentUserId)?.User;
                if (activeOther != null)
                    return activeOther.DisplayName;

                // Si la otra persona se salió, no estará en Participants. Buscamos su rastro en ChatMessages.
                var historicOtherUserId = _db.ChatMessages
                    .Where(m => m.ChatId == chat.Id && m.SenderId != currentUserId)
                    .Select(m => m.SenderId)
                    .FirstOrDefault();

                if (historicOtherUserId != Guid.Empty)
                {
                    var user = _db.Users.FirstOrDefault(u => u.Id == historicOtherUserId);
                    if (user != null) return user.DisplayName;
                }

                return "Conversación";
            }

            // 3. Fallback para grupos sin nombre asignado
            var groupOthers = chat.Participants
                .Where(p => p.UserId != currentUserId)
                .Select(p => p.User?.DisplayName ?? "Usuario")
                .ToList();

            if (groupOthers.Count == 0) return "Chat grupal";
            if (groupOthers.Count <= 2) return string.Join(", ", groupOthers);
            return $"{string.Join(", ", groupOthers.Take(2))} y {groupOthers.Count - 2} más";
        }

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max).TrimEnd() + "…");

        public async Task<List<StudyGo.Models.User>> SearchUsersByEmailAsync(string emailQuery, Guid currentUserId)
        {
            if (string.IsNullOrWhiteSpace(emailQuery))
                return new List<StudyGo.Models.User>();

            return await _db.Users
                .Where(u => u.Id != currentUserId && (u.Email.Contains(emailQuery) || u.DisplayName.Contains(emailQuery)))
                .Take(5)
                .ToListAsync();
        }

        public async Task<bool> DeleteMessageAsync(Guid messageId, Guid currentUserId)
        {
            var message = await _db.ChatMessages.FirstOrDefaultAsync(m => m.Id == messageId);

            // Validamos que el mensaje exista, no esté borrado ya, y que quien borra sea el autor original
            if (message is null || message.IsDeleted || message.SenderId != currentUserId)
                return false;

            message.IsDeleted = true;
            await _db.SaveChangesAsync();

            return true;
        }

        // En Services/ChatService.cs
        public async Task<Guid> CreateGroupChatAsync(List<Guid> participantIds, Guid creatorId, string groupName)
        {
            // 1. Aseguramos que el creador esté incluido en la lista de participantes y eliminamos duplicados
            var allParticipants = participantIds.Append(creatorId).Distinct().ToList();

            if (allParticipants.Count < 2)
                throw new ArgumentException("Un chat grupal debe tener al menos 2 participantes.");

            var newChat = new Chat
            {
                Id = Guid.NewGuid(),
                Type = ChatType.Grupal,
                Name = string.IsNullOrWhiteSpace(groupName) ? "Nuevo Chat Grupal" : groupName.Trim()
            };
            _db.Chats.Add(newChat);

            var participantsEntities = allParticipants.Select(userId => new ChatParticipant
            {
                ChatId = newChat.Id,
                UserId = userId
            }).ToList();

            _db.ChatParticipants.AddRange(participantsEntities);

            await _db.SaveChangesAsync();
            return newChat.Id;
        }

        public async Task<(bool Success, string? LeavingUserName, Guid? ChatId)> RemoveChatForUserAsync(Guid chatId, Guid userId)
        {
            // 1. Buscamos al participante incluyendo sus datos de usuario
            var participant = await _db.ChatParticipants
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);

            if (participant == null)
                return (false, null, null);

            string userName = participant.User?.DisplayName ?? "Un usuario";

            // 2. Agregamos el mensaje del sistema automático a la conversación
            var systemMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = userId, // Usamos su ID como emisor físico para no romper la clave foránea
                EncryptedContent = _cipher.Encrypt($"📢 {userName} ha abandonado la conversación."),
                SentAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(systemMessage);

            // 3. Removemos al usuario del chat para que ya no aparezca en su barra lateral
            _db.ChatParticipants.Remove(participant);

            await _db.SaveChangesAsync();

            return (true, userName, chatId);
        }
    }
}