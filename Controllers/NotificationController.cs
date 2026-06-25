// ============================================================================
// StudyGo · Controllers/NotificationController.cs
// ============================================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using StudyGo.Hubs;
using StudyGo.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using StudyGo.ViewModels.Notifications;

namespace StudyGo.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationController(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var viewModel = new NotificationListViewModel
            {
                UnreadCount = 2,
                Notifications = new List<NotificationItemViewModel>()
            };
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult GetLatest()
        {
            var latest = new List<object>
            {
                new { Id = 1, Type = "info", Message = "Nueva tarea: Algoritmos", TimeRel = "Hace 5m", Link = "/Tasks/1" },
                new { Id = 2, Type = "success", Message = "Calificación publicada", TimeRel = "Hace 1h", Link = "/Grades" }
            };
            return Json(new { unreadCount = 2, data = latest });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAsRead(int id)
        {
            return Ok();
        }

        // ============================================================================
        // ACCIÓN AGREGADA: Maneja el POST del botón "Marcar todas leídas" del Centro de Notificaciones
        // ============================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAllAsRead()
        {
            // TODO: Agregar aquí la llamada al _notificationService cuando esté mapeado en BD
            return RedirectToAction(nameof(Index));
        }
    }
}
