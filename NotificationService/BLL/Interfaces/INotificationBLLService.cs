namespace NotificationService.BLL.Interfaces
{
    public interface INotificationBLLService
    {
        Task SendOrderConfirmationAsync(int userId, int orderId, string email);
        Task SendOrderRejectionAsync(int userId, int orderId, string email);
    }
}
