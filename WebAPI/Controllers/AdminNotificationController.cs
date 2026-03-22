using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Pastella.Backend.Application.Services;

namespace Pastella.Backend.WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/notifications")]
    [Authorize(Roles = "Admin")]
    public class AdminNotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public AdminNotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // 🎉 Promosyon Bildirimi Gönder
        [HttpPost("promotion")]
        public async Task<IActionResult> SendPromotionNotification([FromBody] PromotionNotificationRequest request)
        {
            try
            {
                await _notificationService.SendPromotionNotification(request.Title, request.Message, request.UserIds);
                return Ok(new { Message = "Promosyon bildirimi başarıyla gönderildi!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Bildirim gönderilirken hata oluştu", Error = ex.Message });
            }
        }

        // 🎂 Doğum Günü Hatırlatması Gönder
        [HttpPost("birthday-reminder")]
        public async Task<IActionResult> SendBirthdayReminder([FromBody] BirthdayReminderRequest request)
        {
            try
            {
                await _notificationService.SendBirthdayReminder(request.UserId, request.CustomerName, request.BirthdayDate);
                return Ok(new { Message = "Doğum günü hatırlatması gönderildi!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Hatırlatma gönderilirken hata oluştu", Error = ex.Message });
            }
        }

        // 🚚 Teslimat Bildirimi Gönder
        [HttpPost("delivery")]
        public async Task<IActionResult> SendDeliveryNotification([FromBody] DeliveryNotificationRequest request)
        {
            try
            {
                await _notificationService.SendDeliveryNotification(request.UserId, request.OrderId, request.DeliveryStatus);
                return Ok(new { Message = "Teslimat bildirimi gönderildi!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Teslimat bildirimi gönderilirken hata oluştu", Error = ex.Message });
            }
        }
    }

    // Request DTOs
    public class PromotionNotificationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<int>? UserIds { get; set; }
    }

    public class BirthdayReminderRequest
    {
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime BirthdayDate { get; set; }
    }

    public class DeliveryNotificationRequest
    {
        public int UserId { get; set; }
        public int OrderId { get; set; }
        public string DeliveryStatus { get; set; } = string.Empty;
    }
}