// ============================================================================
// StudyGo · Services/INotificationService.cs + NotificationService.cs
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using StudyGo.Models;
using StudyGo.ViewModels.Notifications;

namespace StudyGo.Services
{
    public interface INotificationService
    {
        Task<NotificationDropdownViewModel> GetDropdownAsync(Guid userId, int take = 10);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkAsReadAsync(Guid notificationId, Guid userId);
        Task MarkAllAsReadAsync(Guid userId);
        Task<NotificationItemViewModel> CreateAsync(Guid userId, string type, string message, string? link = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;

        public NotificationService(AppDbContext db) => _db = db;

        public async Task<NotificationDropdownViewModel> GetDropdownAsync(Guid userId, int take = 10)
        {
            var total = take + 1; // pedimos uno extra para saber si hay más
            var items = await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(total)
                .ToListAsync();

            var hasMore = items.Count > take;
            if (hasMore) items = items.Take(take).ToList();

            var unread = await _db.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return new NotificationDropdownViewModel
            {
                Items = items.Select(ToViewModel).ToList(),
                UnreadCount = unread,
                HasMore = hasMore,
            };
        }

        public async Task<int> GetUnreadCountAsync(Guid userId) =>
            await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId);
            if (n is null || n.IsRead) return;
            n.IsRead = true;
            await _db.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var unread = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            foreach (var n in unread) n.IsRead = true;
            await _db.SaveChangesAsync();
        }

        public async Task<NotificationItemViewModel> CreateAsync(Guid userId, string type, string message, string? link = null)
        {
            var entity = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Message = message,
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
            };
            _db.Notifications.Add(entity);
            await _db.SaveChangesAsync();
            return ToViewModel(entity);
        }

        private static NotificationItemViewModel ToViewModel(Notification n) => new()
        {
            Id = n.Id,
            Type = n.Type,
            Message = n.Message,
            Link = n.Link,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
        };
    }
}
