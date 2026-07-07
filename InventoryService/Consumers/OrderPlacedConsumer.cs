using MassTransit;
using SerilogLogContext = Serilog.Context.LogContext;
using InventoryService.BLL.Interfaces;
using InventoryService.Contracts;

namespace InventoryService.Consumers
{
    public class OrderPlacedConsumer : IConsumer<IOrderPlaced>
    {
        private readonly IInventoryBLLService _inventoryService;
        private readonly ILogger<OrderPlacedConsumer> _logger;

        public OrderPlacedConsumer(IInventoryBLLService inventoryService, ILogger<OrderPlacedConsumer> logger)
        {
            _inventoryService = inventoryService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IOrderPlaced> context)
        {
            var correlationId = context.Headers.TryGetHeader("CorrelationId", out var headerValue)
                ? headerValue?.ToString()
                : null;

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = context.Message.OrderId.ToString();
            }

            using (SerilogLogContext.PushProperty("CorrelationId", correlationId))
            {
                var orderId = context.Message.OrderId;
                var reservedItems = new List<IOrderItemDto>();

                try
                {
                    foreach (var item in context.Message.Items)
                    {
                        _logger.LogInformation("Reserving {Quantity} units of product {ProductId} for order {OrderId}", item.Quantity, item.ProductId, orderId);
                        var reserved = await _inventoryService.ReserveAsync(item.ProductId, item.Quantity);
                        if (!reserved)
                            throw new InvalidOperationException($"Insufficient inventory for product {item.ProductId}");

                        reservedItems.Add(item);
                    }

                    _logger.LogInformation("Inventory reserved for order {OrderId}", orderId);
                    await context.Publish<IInventoryReserved>(new
                    {
                        OrderId = orderId
                    }, publishContext => publishContext.Headers.Set("CorrelationId", correlationId));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Inventory reservation failed for order {OrderId}. Releasing reserved items.", orderId);
                    foreach (var reservedItem in reservedItems)
                    {
                        await _inventoryService.ReleaseAsync(reservedItem.ProductId, reservedItem.Quantity);
                    }

                    await context.Publish<IInventoryRejected>(new
                    {
                        OrderId = orderId,
                        Reason = ex.Message
                    }, publishContext => publishContext.Headers.Set("CorrelationId", correlationId));
                }
            }
        }
    }
}
