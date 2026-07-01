using Microsoft.AspNetCore.Mvc;
using NotificationService.BLL.Interfaces;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationBLLService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationBLLService notificationService, ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpPost("order-confirmed")]
        public async Task<ActionResult> NotifyOrderConfirmed([FromBody] OrderNotificationRequest request)
        {
            await _notificationService.SendOrderConfirmationAsync(request.UserId, request.OrderId, request.Email);
            return Ok("Notification sent");
        }

        [HttpPost("order-rejected")]
        public async Task<ActionResult> NotifyOrderRejected([FromBody] OrderNotificationRequest request)
        {
            await _notificationService.SendOrderRejectionAsync(request.UserId, request.OrderId, request.Email);
            return Ok("Notification sent");
        }
    }

    public class OrderNotificationRequest
    {
        public int UserId { get; set; }
        public int OrderId { get; set; }
        public string Email { get; set; } = null!;
    }
}
