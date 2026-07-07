using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OrderService.BLL.Interfaces;
using OrderService.Contracts;
using OrderService.DAL.Interfaces;
using OrderService.Models;

namespace OrderService.BLL
{
    public class OrderBLLService : IOrderBLLService
    {
        private readonly IOrderDAL _orderDAL;
        private readonly ILogger<OrderBLLService> _logger;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderBLLService(IOrderDAL orderDAL, ILogger<OrderBLLService> logger, IPublishEndpoint publishEndpoint, IHttpContextAccessor httpContextAccessor)
        {
            _orderDAL = orderDAL;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetCorrelationId()
        {
            var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
            return string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString() : correlationId;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _orderDAL.GetOrderByIdAsync(orderId);
        }

        public async Task<List<Order>> GetUserOrdersAsync(int userId)
        {
            return await _orderDAL.GetOrdersByUserIdAsync(userId);
        }

        public async Task<Order> CreateOrderAsync(int userId, List<(int productId, int quantity, decimal price)> items)
        {
            var order = new Order
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                TotalPrice = 0,
                Items = new List<OrderItem>()
            };

            foreach (var item in items)
            {
                var orderItem = new OrderItem
                {
                    ProductId = item.productId,
                    Quantity = item.quantity,
                    Price = item.price
                };
                order.Items.Add(orderItem);
                order.TotalPrice += item.price * item.quantity;
            }

            _logger.LogInformation($"Creating order for user {userId} with {items.Count} items");
            var createdOrder = await _orderDAL.CreateOrderAsync(order);

            var correlationId = GetCorrelationId();
            _logger.LogInformation($"Publishing OrderPlaced event for order {createdOrder.Id}");
            await _publishEndpoint.Publish<IOrderPlaced>(new
            {
                OrderId = createdOrder.Id,
                UserId = createdOrder.UserId,
                TotalPrice = createdOrder.TotalPrice,
                Items = createdOrder.Items.Select(item => new
                {
                    item.ProductId,
                    item.Quantity
                }).ToList()
            }, context => context.Headers.Set("CorrelationId", correlationId));

            return createdOrder;
        }

        public async Task<Order> ConfirmOrderAsync(int orderId)
        {
            var correlationId = GetCorrelationId();
            var order = await _orderDAL.GetOrderByIdAsync(orderId);
            if (order == null)
                throw new Exception($"Order {orderId} not found");

            if (order.Status == "Confirmed")
                return order;

            order.Status = "Confirmed";
            _logger.LogInformation($"Manually confirming order {orderId}");
            var updated = await _orderDAL.UpdateOrderAsync(order);

            _logger.LogInformation($"Publishing OrderFinalized event for order {orderId}");
            await _publishEndpoint.Publish<IOrderFinalized>(new
            {
                OrderId = updated.Id,
                UserId = updated.UserId,
                Status = updated.Status
            }, context => context.Headers.Set("CorrelationId", correlationId));

            return updated;
        }

        public async Task<Order> RejectOrderAsync(int orderId)
        {
            var correlationId = GetCorrelationId();
            var order = await _orderDAL.GetOrderByIdAsync(orderId);
            if (order == null)
                throw new Exception($"Order {orderId} not found");

            order.Status = "Rejected";
            _logger.LogInformation($"Rejecting order {orderId}");
            var updated = await _orderDAL.UpdateOrderAsync(order);

            _logger.LogInformation($"Publishing OrderFinalized event for order {orderId}");
            await _publishEndpoint.Publish<IOrderFinalized>(new
            {
                OrderId = updated.Id,
                UserId = updated.UserId,
                Status = updated.Status
            }, context => context.Headers.Set("CorrelationId", correlationId));

            return updated;
        }
    }
}