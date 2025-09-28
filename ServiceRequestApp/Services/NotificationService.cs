using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ServiceRequestApp.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string userId, string title, string message, string type = "info");
        Task SendEmailNotificationAsync(string email, string subject, string body);
        Task SendSMSNotificationAsync(string phoneNumber, string message);
        Task<List<Notification>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 10);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public NotificationService(ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task SendNotificationAsync(string userId, string title, string message, string type = "info")
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();

            // Here you can add SignalR or other real-time notification systems
            // await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification);
        }

        public async Task SendEmailNotificationAsync(string email, string subject, string body)
        {
            // Implement email sending logic here
            // You can use SendGrid, SMTP, or other email services
            try
            {
                // Placeholder for email sending
                Console.WriteLine($"Email sent to {email}: {subject}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
            }
        }

        public async Task SendSMSNotificationAsync(string phoneNumber, string message)
        {
            // Implement SMS sending logic here
            // You can use Twilio, SMS Gateway, or other SMS services
            try
            {
                // Placeholder for SMS sending
                Console.WriteLine($"SMS sent to {phoneNumber}: {message}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMS sending failed: {ex.Message}");
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _dbContext.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _dbContext.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _dbContext.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }
    }

    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // info, success, warning, error
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? ActionUrl { get; set; }
        public string? Icon { get; set; }
        
        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }
}
