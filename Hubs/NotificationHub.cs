// ============================================================================
// StudyGo · Hubs/NotificationHub.cs — push de notificaciones en tiempo real
// ============================================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace StudyGo.Hubs // <-- CORREGIDO: Cambiado de StudyGo.Web.Hubs a StudyGo.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
    }
}