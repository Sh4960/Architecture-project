using OrderService.BLL.Interfaces;
using OrderService.DAL.Interfaces;
using OrderService.Models;

namespace OrderService.BLL
{
    public class OrderBLLService : IOrderBLLService
    {
        private readonly IOrderDAL _orderDAL;
        private readonly ILogger<OrderBLLService> _logger;

        public OrderBLLService(IOrderDAL orderDAL, ILogger<OrderBLLService> logger)
        {
            _orderDAL = orderDAL;
            _logger = logger;
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
            return await _orderDAL.CreateOrderAsync(order);
        }

        public async Task<Order> ConfirmOrderAsync(int orderId)
        {
            var order = await _orderDAL.GetOrderByIdAsync(orderId);
            if (order == null)
                throw new Exception($"Order {orderId} not found");

            order.Status = "Confirmed";
            _logger.LogInformation($"Confirming order {orderId}");
            return await _orderDAL.UpdateOrderAsync(order);
        }

        public async Task<Order> RejectOrderAsync(int orderId)
        {
            var order = await _orderDAL.GetOrderByIdAsync(orderId);
            if (order == null)
                throw new Exception($"Order {orderId} not found");

            order.Status = "Rejected";
            _logger.LogInformation($"Rejecting order {orderId}");
            return await _orderDAL.UpdateOrderAsync(order);
        }
    }
}
