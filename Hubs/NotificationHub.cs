// ============================================================================
// StudyGo · Hubs/NotificationHub.cs — push de notificaciones en tiempo real
// Cada usuario se une a su propio grupo "notif:{userId}".
// El servidor envía ReceiveNotification cuando llega una nueva.
// ============================================================================
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using StudyGo.Services;

namespace StudyGo.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ICurrentUserResolver _currentUser;

        public NotificationHub(ICurrentUserResolver currentUser) => _currentUser = currentUser;

        private static string Group(Guid userId) => $"notif:{userId}";

        public override async Task OnConnectedAsync()
        {
            var me = await _currentUser.ResolveAsync(Context.User);
            if (me is not null)
                await Groups.AddToGroupAsync(Context.ConnectionId, Group(me.Id));
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Helper estático para enviar una notificación a un usuario desde cualquier servicio.
        /// Uso: await hub.Clients.Group($"notif:{userId}").SendAsync("ReceiveNotification", item);
        /// </summary>
        public static string GroupName(Guid userId) => Group(userId);
    }
}
