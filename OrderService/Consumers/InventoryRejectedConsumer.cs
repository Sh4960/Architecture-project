using MassTransit;
using SerilogLogContext = Serilog.Context.LogContext;
using OrderService.Contracts;
using OrderService.DAL.Interfaces;

namespace OrderService.Consumers
{
    public class InventoryRejectedConsumer : IConsumer<IInventoryRejected>
    {
        private readonly IOrderDAL _orderDal;
        private readonly ILogger<InventoryRejectedConsumer> _logger;

        public InventoryRejectedConsumer(IOrderDAL orderDal, ILogger<InventoryRejectedConsumer> logger)
        {
            _orderDal = orderDal;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IInventoryRejected> context)
        {
            var correlationId = context.Headers.TryGetHeader("CorrelationId", out var headerValue)
                ? headerValue?.ToString()
                : context.Message.OrderId.ToString();

            using (SerilogLogContext.PushProperty("CorrelationId", correlationId))
            {
                var order = await _orderDal.GetOrderByIdAsync(context.Message.OrderId);
                if (order == null)
                {
                    _logger.LogWarning("Received InventoryRejected for unknown order {OrderId}", context.Message.OrderId);
                    return;
                }

                if (order.Status == "Rejected")
                {
                    _logger.LogInformation("Order {OrderId} already rejected", order.Id);
                    return;
                }

                order.Status = "Rejected";
                await _orderDal.UpdateOrderAsync(order);
                _logger.LogInformation("Order {OrderId} rejected by saga: {Reason}", order.Id, context.Message.Reason);

                await context.Publish<IOrderFinalized>(new
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    Status = order.Status
                }, publishContext => publishContext.Headers.Set("CorrelationId", correlationId));
            }
        }
    }
}
