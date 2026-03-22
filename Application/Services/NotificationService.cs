using Pastella.Backend.Core.DTOs;
using Pastella.Backend.Core.Entities;
using Pastella.Backend.Core.Interfaces;
using Pastella.Backend.Infrastructure.Repositories;

namespace Pastella.Backend.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IRepository<Notification> _notificationRepository;
        private readonly NotificationRepository _notificationRepo;
        private readonly IFCMService _fcmService;
        private readonly IUserRepository _userRepository; // Yeni ekleme

        public NotificationService(IRepository<Notification> notificationRepository, IFCMService fcmService, IUserRepository userRepository)
        {
            _notificationRepository = notificationRepository;
            _notificationRepo = (NotificationRepository)notificationRepository;
            _fcmService = fcmService;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotifications(int userId)
        {
            var notifications = await _notificationRepo.GetUserNotifications(userId);
            return notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Message = n.Message,
                CreatedDate = n.CreatedDate,
                IsRead = n.IsRead
            });
        }

        public async Task<bool> MarkAsRead(int notificationId)
        {
            var notification = await _notificationRepository.GetById(notificationId);
            if (notification == null) return false;

            notification.IsRead = true;
            await _notificationRepository.Update(notificationId, notification);
            return true;
        }

        public async Task CreateNotification(int userId, string type, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Message = message,
                CreatedDate = DateTime.UtcNow,
                IsRead = false
            };

            await _notificationRepository.Create(notification);
        }

        // 🎉 Promosyon Bildirimleri
        public async Task SendPromotionNotification(string title, string message, List<int>? userIds = null)
        {
            if (userIds == null || !userIds.Any())
            {
                // Tüm kullanıcılara gönder
                var allUsers = await GetAllActiveUsers();
                userIds = allUsers.Select(u => u.Id).ToList();
            }

            foreach (var userId in userIds)
            {
                await CreateNotification(userId, "PROMOTION", message);
                await _fcmService.SendToUser(userId, title, message, 
                    new Dictionary<string, string> { { "type", "promotion" } });
            }
        }

        // 🎂 Doğum Günü Hatırlatmaları
        public async Task SendBirthdayReminder(int userId, string customerName, DateTime birthdayDate)
        {
            var daysUntil = (birthdayDate - DateTime.Now).Days;
            var message = daysUntil switch
            {
                0 => $"🎉 Bugün {customerName}'in doğum günü! Özel pasta siparişi vermeyi unutmayın!",
                1 => $"🎂 Yarın {customerName}'in doğum günü! Hemen pasta sipariş edin!",
                7 => $"📅 {customerName}'in doğum günü 1 hafta sonra. Pasta siparişi için zamanı geldi!",
                _ => $"🎈 {customerName}'in doğum günü {daysUntil} gün sonra!"
            };

            await CreateNotification(userId, "BIRTHDAY_REMINDER", message);
            await _fcmService.SendToUser(userId, "Pastella - Doğum Günü Hatırlatması", message,
                new Dictionary<string, string> 
                { 
                    { "type", "birthday_reminder" },
                    { "customerName", customerName },
                    { "birthdayDate", birthdayDate.ToString("yyyy-MM-dd") }
                });
        }

        // 🚚 Teslimat Bildirimleri
        public async Task SendDeliveryNotification(int userId, int orderId, string deliveryStatus)
        {
            var message = deliveryStatus switch
            {
                "COURIER_ASSIGNED" => "Kurye atandı! Siparişiniz yakında yola çıkacak.",
                "OUT_FOR_DELIVERY" => "Kurye yola çıktı! Siparişiniz size doğru geliyor! 🚚",
                "NEARBY" => "Kurye yakınınızda! Lütfen hazır olun.",
                "DELIVERED" => "Siparişiniz teslim edildi! Afiyet olsun! 🎉",
                _ => $"Teslimat durumu: {deliveryStatus}"
            };

            await CreateNotification(userId, "DELIVERY_STATUS", message);
            await _fcmService.SendToUser(userId, "Pastella - Teslimat Güncellemesi", message,
                new Dictionary<string, string> 
                { 
                    { "type", "delivery_status" },
                    { "orderId", orderId.ToString() },
                    { "status", deliveryStatus }
                });
        }

        private async Task<List<User>> GetAllActiveUsers()
        {
            var users = await _userRepository.GetAll();
            return users.ToList();
        }
    }
}