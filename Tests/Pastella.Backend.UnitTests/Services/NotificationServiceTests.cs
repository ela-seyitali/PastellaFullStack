using FluentAssertions;
using Moq;
using Pastella.Backend.Application.Services;
using Pastella.Backend.Core.Entities;
using Pastella.Backend.Core.Interfaces;
using Pastella.Backend.Infrastructure.Data;
using Pastella.Backend.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Pastella.Backend.UnitTests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<IFCMService> _fcmServiceMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly NotificationService _notificationService;
        private readonly ApplicationDbContext _context;
        private readonly NotificationRepository _notificationRepository;

        public NotificationServiceTests()
        {
            // In-memory database setup
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _notificationRepository = new NotificationRepository(_context);
            _fcmServiceMock = new Mock<IFCMService>();
            _userRepositoryMock = new Mock<IUserRepository>();
            
            _notificationService = new NotificationService(
                _notificationRepository,
                _fcmServiceMock.Object,
                _userRepositoryMock.Object
            );
        }

        [Fact]
        public async Task CreateNotification_ShouldCreateNotificationSuccessfully()
        {
            // Arrange
            int userId = 1;
            string type = "ORDER_STATUS";
            string message = "Your order has been confirmed";

            // Act
            await _notificationService.CreateNotification(userId, type, message);

            // Assert
            var notifications = await _context.Notifications.ToListAsync();
            notifications.Should().HaveCount(1);
            notifications[0].UserId.Should().Be(userId);
            notifications[0].Type.Should().Be(type);
            notifications[0].Message.Should().Be(message);
            notifications[0].IsRead.Should().BeFalse();
        }

        [Fact]
        public async Task MarkAsRead_WithValidId_ShouldMarkNotificationAsRead()
        {
            // Arrange
            // Add user first to satisfy foreign key
            var user = new User
            {
                Id = 1,
                Email = "test@test.com",
                PasswordHash = "hash",
                Role = "Customer"
            };
            await _context.Users.AddAsync(user);
            
            var notification = new Notification
            {
                UserId = 1,
                Type = "ORDER_STATUS",
                Message = "Test message",
                IsRead = false,
                CreatedDate = DateTime.UtcNow
            };
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
            
            var savedId = notification.Id;
            
            // Clear context to ensure fresh load
            _context.ChangeTracker.Clear();

            // Act
            var result = await _notificationService.MarkAsRead(savedId);

            // Assert
            result.Should().BeTrue();
            
            // Reload from database to get updated state
            var updatedNotification = await _context.Notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == savedId);
            updatedNotification.Should().NotBeNull();
            updatedNotification!.IsRead.Should().BeTrue();
        }

        [Fact]
        public async Task MarkAsRead_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            int notificationId = 999;

            // Act
            var result = await _notificationService.MarkAsRead(notificationId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SendPromotionNotification_WithSpecificUsers_ShouldSendToSpecifiedUsers()
        {
            // Arrange
            string title = "Special Offer";
            string message = "50% off on all cakes!";
            var userIds = new List<int> { 1, 2, 3 };

            _fcmServiceMock
                .Setup(x => x.SendToUser(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(true);

            // Act
            await _notificationService.SendPromotionNotification(title, message, userIds);

            // Assert
            var notifications = await _context.Notifications.ToListAsync();
            notifications.Should().HaveCount(3);
            notifications.All(n => n.Type == "PROMOTION").Should().BeTrue();
            _fcmServiceMock.Verify(x => x.SendToUser(It.IsAny<int>(), title, message, It.IsAny<Dictionary<string, string>>()), Times.Exactly(3));
        }

        [Fact]
        public async Task SendPromotionNotification_WithoutUserIds_ShouldSendToAllUsers()
        {
            // Arrange
            string title = "Special Offer";
            string message = "50% off on all cakes!";
            var allUsers = new List<User>
            {
                new User { Id = 1, Email = "user1@test.com", PasswordHash = "hash1", Role = "Customer" },
                new User { Id = 2, Email = "user2@test.com", PasswordHash = "hash2", Role = "Customer" },
                new User { Id = 3, Email = "user3@test.com", PasswordHash = "hash3", Role = "Customer" }
            };

            _userRepositoryMock
                .Setup(x => x.GetAll())
                .ReturnsAsync(allUsers);

            _fcmServiceMock
                .Setup(x => x.SendToUser(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(true);

            // Act
            await _notificationService.SendPromotionNotification(title, message, null);

            // Assert
            var notifications = await _context.Notifications.ToListAsync();
            notifications.Should().HaveCount(3);
            _fcmServiceMock.Verify(x => x.SendToUser(It.IsAny<int>(), title, message, It.IsAny<Dictionary<string, string>>()), Times.Exactly(3));
        }

        [Theory]
        [InlineData(0, "🎉 Bugün Test User'in doğum günü! Özel pasta siparişi vermeyi unutmayın!")]
        [InlineData(1, "🎂 Yarın Test User'in doğum günü! Hemen pasta sipariş edin!")]
        [InlineData(7, "📅 Test User'in doğum günü 1 hafta sonra. Pasta siparişi için zamanı geldi!")]
        public async Task SendBirthdayReminder_ShouldSendCorrectMessageBasedOnDaysUntil(int daysUntil, string expectedMessage)
        {
            // Arrange
            int userId = 1;
            string customerName = "Test User";
            DateTime birthdayDate = DateTime.Now.AddDays(daysUntil);

            _fcmServiceMock
                .Setup(x => x.SendToUser(userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(true);

            // Act
            await _notificationService.SendBirthdayReminder(userId, customerName, birthdayDate);

            // Assert
            var notifications = await _context.Notifications.ToListAsync();
            notifications.Should().HaveCount(1);
            notifications[0].UserId.Should().Be(userId);
            notifications[0].Type.Should().Be("BIRTHDAY_REMINDER");
            notifications[0].Message.Should().Contain(customerName);
            notifications[0].Message.Should().Contain("doğum günü");

            _fcmServiceMock.Verify(x => x.SendToUser(
                userId, 
                "Pastella - Doğum Günü Hatırlatması", 
                It.IsAny<string>(), 
                It.IsAny<Dictionary<string, string>>()
            ), Times.Once);
        }

        [Theory]
        [InlineData("COURIER_ASSIGNED", "Kurye atandı! Siparişiniz yakında yola çıkacak.")]
        [InlineData("OUT_FOR_DELIVERY", "Kurye yola çıktı! Siparişiniz size doğru geliyor! 🚚")]
        [InlineData("NEARBY", "Kurye yakınınızda! Lütfen hazır olun.")]
        [InlineData("DELIVERED", "Siparişiniz teslim edildi! Afiyet olsun! 🎉")]
        public async Task SendDeliveryNotification_ShouldSendCorrectMessageBasedOnStatus(string deliveryStatus, string expectedMessage)
        {
            // Arrange
            int userId = 1;
            int orderId = 100;

            _fcmServiceMock
                .Setup(x => x.SendToUser(userId, It.IsAny<string>(), expectedMessage, It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(true);

            // Act
            await _notificationService.SendDeliveryNotification(userId, orderId, deliveryStatus);

            // Assert
            var notifications = await _context.Notifications.ToListAsync();
            notifications.Should().HaveCount(1);
            notifications[0].UserId.Should().Be(userId);
            notifications[0].Type.Should().Be("DELIVERY_STATUS");
            notifications[0].Message.Should().Be(expectedMessage);

            _fcmServiceMock.Verify(x => x.SendToUser(
                userId, 
                "Pastella - Teslimat Güncellemesi", 
                expectedMessage, 
                It.Is<Dictionary<string, string>>(d => 
                    d["type"] == "delivery_status" && 
                    d["orderId"] == orderId.ToString() &&
                    d["status"] == deliveryStatus
                )
            ), Times.Once);
        }
    }
}
