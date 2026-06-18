// ============================================================================
// StudyGo · Controllers/NotificationController.cs — módulo Comunicación (Jaison)
// ============================================================================
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using StudyGo.Hubs;
using StudyGo.Services;

namespace StudyGo.Controllers
{
    public class NotificationController : Controller
    {
        private readonly INotificationService _notif;
        private readonly ICurrentUserResolver _currentUser;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationController(
            INotificationService notif,
            ICurrentUserResolver currentUser,
            IHubContext<NotificationHub> hub)
        {
            _notif = notif;
            _currentUser = currentUser;
            _hub = hub;
        }

        // GET /Notification/Dropdown — JSON para el dropdown de la campana
        [HttpGet]
        public async Task<IActionResult> Dropdown()
        {
            var me = await _currentUser.ResolveAsync(User);
            if (me is null) return Unauthorized();
            var vm = await _notif.GetDropdownAsync(me.Id);
            return Json(vm);
        }

        // GET /Notification/UnreadCount — contador de no leídos
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var me = await _currentUser.ResolveAsync(User);
            if (me is null) return Json(new { count = 0 });
            var count = await _notif.GetUnreadCountAsync(me.Id);
            return Json(new { count });
        }

        // POST /Notification/MarkRead/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            var me = await _currentUser.ResolveAsync(User);
            if (me is null) return Unauthorized();
            await _notif.MarkAsReadAsync(id, me.Id);
            return Json(new { ok = true });
        }

        // POST /Notification/MarkAllRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var me = await _currentUser.ResolveAsync(User);
            if (me is null) return Unauthorized();
            await _notif.MarkAllAsReadAsync(me.Id);
            return Json(new { ok = true });
        }

        // POST /Notification/Test — crea una notificación de prueba y la pushea por SignalR
        [HttpPost]
        public async Task<IActionResult> Test()
        {
            var me = await _currentUser.ResolveAsync(User);
            if (me is null) return Unauthorized();

            var item = await _notif.CreateAsync(
                me.Id,
                "NuevoMensaje",
                "Esta es una notificación de prueba en tiempo real.",
                "/Chat"
            );

            // Push al cliente vía SignalR
            await _hub.Clients
                .Group(NotificationHub.GroupName(me.Id))
                .SendAsync("ReceiveNotification", item);

            return Json(new { ok = true });
        }
    }
}
