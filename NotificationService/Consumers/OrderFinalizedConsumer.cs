using MassTransit;
using SerilogLogContext = Serilog.Context.LogContext;
using NotificationService.BLL.Interfaces;
using NotificationService.Contracts;

namespace NotificationService.Consumers
{
    public class OrderFinalizedConsumer : IConsumer<IOrderFinalized>
    {
        private readonly INotificationBLLService _notificationService;
        private readonly ILogger<OrderFinalizedConsumer> _logger;

        public OrderFinalizedConsumer(INotificationBLLService notificationService, ILogger<OrderFinalizedConsumer> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IOrderFinalized> context)
        {
            var correlationId = context.Headers.TryGetHeader("CorrelationId", out var headerValue)
                ? headerValue?.ToString()
                : context.Message.OrderId.ToString();

            using (SerilogLogContext.PushProperty("CorrelationId", correlationId))
            {
                var message = context.Message;
                var email = $"user{message.UserId}@example.com";

                if (message.Status == "Confirmed")
                {
                    _logger.LogInformation("Sending order confirmation notification for order {OrderId}", message.OrderId);
                    await _notificationService.SendOrderConfirmationAsync(message.UserId, message.OrderId, email);
                }
                else
                {
                    _logger.LogInformation("Sending order rejection notification for order {OrderId}", message.OrderId);
                    await _notificationService.SendOrderRejectionAsync(message.UserId, message.OrderId, email);
                }
            }
        }
    }
}
