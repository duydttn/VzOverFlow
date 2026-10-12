using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using VzOverFlow.Data;
using VzOverFlow.Hubs;
using VzOverFlow.Models;

namespace VzOverFlow.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateNotificationAsync(int userId, string message, string link)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Link = link,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send Real-time
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                message = notification.Message,
                link = notification.Link,
                createdAt = notification.CreatedAt.ToString("HH:mm"),
                isRead = false
            });
        }

        public async Task NotifyAnswerAsync(int questionOwnerId, string questionTitle, int questionId, string answererName)
        {
            var message = $"{answererName} đã trả lời câu hỏi của bạn: {Truncate(questionTitle)}";
            var link = $"/Questions/Details/{questionId}";
            await CreateNotificationAsync(questionOwnerId, message, link);
        }

        public async Task NotifyAcceptAnswerAsync(int answerOwnerId, string questionTitle, int questionId)
        {
            var message = $"Câu trả lời của bạn cho '{Truncate(questionTitle)}' đã được chấp nhận!";
            var link = $"/Questions/Details/{questionId}";
            await CreateNotificationAsync(answerOwnerId, message, link);
        }

        public async Task NotifyFollowAsync(int followedUserId, string followerName, int followerId)
        {
            var message = $"{followerName} đã bắt đầu theo dõi bạn.";
            var link = $"/Users/Profile/{followerId}";
            await CreateNotificationAsync(followedUserId, message, link);
        }

        private static string Truncate(string value, int maxLength = 50)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }
    }
}