using MassTransit;
using SerilogLogContext = Serilog.Context.LogContext;
using OrderService.Contracts;
using OrderService.DAL.Interfaces;

namespace OrderService.Consumers
{
    public class InventoryReservedConsumer : IConsumer<IInventoryReserved>
    {
        private readonly IOrderDAL _orderDal;
        private readonly ILogger<InventoryReservedConsumer> _logger;

        public InventoryReservedConsumer(IOrderDAL orderDal, ILogger<InventoryReservedConsumer> logger)
        {
            _orderDal = orderDal;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IInventoryReserved> context)
        {
            var correlationId = context.Headers.TryGetHeader("CorrelationId", out var headerValue)
                ? headerValue?.ToString()
                : context.Message.OrderId.ToString();

            using (SerilogLogContext.PushProperty("CorrelationId", correlationId))
            {
                var order = await _orderDal.GetOrderByIdAsync(context.Message.OrderId);
                if (order == null)
                {
                    _logger.LogWarning("Received InventoryReserved for unknown order {OrderId}", context.Message.OrderId);
                    return;
                }

                if (order.Status == "Confirmed")
                {
                    _logger.LogInformation("Order {OrderId} already confirmed", order.Id);
                    return;
                }

                order.Status = "Confirmed";
                await _orderDal.UpdateOrderAsync(order);
                _logger.LogInformation("Order {OrderId} confirmed by saga", order.Id);

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
