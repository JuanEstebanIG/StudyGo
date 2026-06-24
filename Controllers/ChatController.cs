// ============================================================================
// StudyGo · Controllers/ChatController.cs — módulo Comunicación (Jaison)
// La vista recibe SIEMPRE un ViewModel armado aquí desde IChatService (§2).
// El envío en vivo va por SignalR (ChatHub); Send() es un respaldo HTTP.
// ============================================================================
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StudyGo.Services;
using StudyGo.ViewModels.Chat;

namespace StudyGo.Controllers
{
    public class ChatController : Controller
    {
        private readonly IChatService _chat;
        private readonly ICurrentUserResolver _currentUser;

        public ChatController(IChatService chat, ICurrentUserResolver currentUser)
        {
            _chat = chat;
            _currentUser = currentUser;
        }

        // GET /Chat  ·  GET /Chat/Index/{id}
        public async Task<IActionResult> Index(Guid? id)
        {
            var me = await _currentUser.ResolveAsync(User);
            var vm = new ChatPageViewModel();

            if (me is null)
            {
                // Sin sesión (auth pendiente de Micky) y sin usuarios en BD.
                ViewData["Title"] = "Chat";
                return View(vm);
            }

            vm.CurrentUserId = me.Id;
            vm.IsTeacher = me.IsTeacher;
            vm.Conversations = await _chat.GetConversationsAsync(me.Id);

            // Conversación a abrir: la pedida por ruta, o la primera disponible.
            var activeId = id ?? (vm.Conversations.Count > 0 ? vm.Conversations[0].ChatId : (Guid?)null);
            if (activeId is Guid cid)
            {
                vm.ActiveConversation = await _chat.GetThreadAsync(cid, me.Id);
                foreach (var c in vm.Conversations)
                    c.IsActive = c.ChatId == cid && vm.ActiveConversation is not null;
            }

            ViewData["Title"] = "Chat";
            return View(vm);
        }

        // GET /Chat/Messages/{id}  → JSON del hilo (lo consume chat.js al cambiar de conversación)
        [HttpGet]
        public async Task<IActionResult> Messages(Guid id)
        {
            var me = await _currentUser.ResolveAsync(User);
            if (me is null) return Unauthorized();

            var thread = await _chat.GetThreadAsync(id, me.Id);
            if (thread is null) return Forbid(); // no participa → privacidad (§8)

            return Json(new
            {
                chatId = thread.ChatId,
                title = thread.Title,
                isPrivate = thread.IsPrivate,
                participants = thread.Participants,
                currentUserId = me.Id,
                messages = thread.Messages, // IsOwn ya viene resuelto por el servicio
            });
        }

        // POST /Chat/Send  → respaldo HTTP si SignalR no está disponible.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(SendMessageInput input)
        {
            if (input is null || input.ChatId == Guid.Empty || string.IsNullOrWhiteSpace(input.Content))
                return BadRequest(new { error = "Escribe un mensaje antes de enviar." });

            var me = await _currentUser.ResolveAsync(User);
            if (me is null) return Unauthorized();
            if (!await _chat.CanAccessAsync(input.ChatId, me.Id)) return Forbid();

            var item = await _chat.AddMessageAsync(input.ChatId, me.Id, input.Content);
            return Json(item);
        }

        // POST /Chat/StartPrivateChat
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartPrivateChat(Guid targetUserId)
        {
            var me = await _currentUser.ResolveAsync(User);
            if (me is null) return Unauthorized();
            if (me.Id == targetUserId) return BadRequest(new { error = "No puedes iniciar un chat privado contigo mismo." });

            try
            {
                // 1. Crea o recupera el chat real en la base de datos a través del servicio
                var chatId = await _chat.GetOrCreatePrivateChatAsync(me.Id, targetUserId);

                // 2. Devolvemos el objeto JSON dinámico. 
                // Pasamos el ID real. El "title" lo dejamos como un fallback seguro, 
                // ya que el JS inmediatamente reescribirá el header al cargar el hilo (loadChatThread)
                return Json(new
                {
                    chatId = chatId,
                    title = "Nueva Conversación",
                    avatarUrl = (string)null
                });
            }
            catch (Exception)
            {
                return BadRequest(new { error = "Error al inicializar el chat base." });
            }
        }

        // GET /Chat/SearchUsers?emailQuery=...
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string emailQuery)
        {
            var me = await _currentUser.ResolveAsync(User);
            if (me is null || string.IsNullOrWhiteSpace(emailQuery))
                return Json(new ArraySegment<object>());

            // Consumimos el método que añadimos en tu ChatService
            var users = await _chat.SearchUsersByEmailAsync(emailQuery, me.Id);

            // Devolvemos solo los datos necesarios para el frontend
            return Json(users.Select(u => new
            {
                id = u.Id,
                displayName = u.DisplayName,
                email = u.Email
            }));
        }
    }
}
