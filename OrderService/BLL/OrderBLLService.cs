using Microsoft.Extensions.Logging;
using OrderService.BLL.Interfaces;
using OrderService.DAL.Interfaces;
using OrderService.Models;
using System.Net.Http.Json;

namespace OrderService.BLL
{
    public class OrderBLLService : IOrderBLLService
    {
        private readonly IOrderDAL _orderDAL;
        private readonly ILogger<OrderBLLService> _logger;
        private readonly HttpClient _httpClient;

        public OrderBLLService(IOrderDAL orderDAL, ILogger<OrderBLLService> logger, HttpClient httpClient)
        {
            _orderDAL = orderDAL;
            _logger = logger;
            _httpClient = httpClient;
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

            try
            {
                _logger.LogInformation($"Attempting to reserve inventory for order {orderId} with {order.Items.Count} types of items");

                // 1. הכנת אובייקט בקשה מרוכז המכיל את כל הפריטים יחד
                var batchReserveRequest = order.Items.Select(item => new
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToList();

                // 2. פנייה יחידה לשירות המלאי בנתיב המקבל רשימה (Batch)
                // משתמש בשם ה-DNS של הקונטיינר בתוך רשת הדוקר
                var response = await _httpClient.PostAsJsonAsync(
                    "http://inventory-service/api/inventories/reserve-batch", 
                    batchReserveRequest
                );

                // 3. בדיקת סטטוס התשובה
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to reserve inventory for order {orderId}: {errorContent}");
                    
                    // במקרה של כישלון - אנו מעדכנים את סטטוס ההזמנה לנדחה (נצרך לחלק 2/3)
                    order.Status = "Rejected";
                    await _orderDAL.UpdateOrderAsync(order);
                    
                    throw new Exception($"Inventory reservation failed: {errorContent}");
                }

                _logger.LogInformation($"Successfully reserved all items for order {orderId}");

                // 4. אם הכל הצליח - אישור ההזמנה ושמירה
                order.Status = "Confirmed";
                _logger.LogInformation($"Confirming order {orderId}");
                return await _orderDAL.UpdateOrderAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error confirming order {orderId}: {ex.Message}");
                throw;
            }
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