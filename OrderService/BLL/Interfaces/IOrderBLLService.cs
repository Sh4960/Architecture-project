using OrderService.Models;

namespace OrderService.BLL.Interfaces
{
    public interface IOrderBLLService
    {
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<List<Order>> GetUserOrdersAsync(int userId);
        Task<Order> CreateOrderAsync(int userId, List<(int productId, int quantity, decimal price)> items);
        Task<Order> ConfirmOrderAsync(int orderId);
        Task<Order> RejectOrderAsync(int orderId);
    }
}
