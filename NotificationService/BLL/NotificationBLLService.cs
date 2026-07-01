using NotificationService.BLL.Interfaces;
using System.Net;
using System.Net.Mail;

namespace NotificationService.BLL
{
    public class NotificationBLLService : INotificationBLLService
    {
        private readonly ILogger<NotificationBLLService> _logger;

        public NotificationBLLService(ILogger<NotificationBLLService> logger)
        {
            _logger = logger;
        }

        public async Task SendOrderConfirmationAsync(int userId, int orderId, string email)
        {
            try
            {
                var subject = $"Order #{orderId} Confirmed";
                var body = $"Dear Customer,\\n\\nYour order #{orderId} has been confirmed!\\n\\nThank you for your purchase.";
                
                _logger.LogInformation($"Sending confirmation email to {email} for order {orderId}");
                await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending confirmation email to {email}");
                throw;
            }
        }

        public async Task SendOrderRejectionAsync(int userId, int orderId, string email)
        {
            try
            {
                var subject = $"Order #{orderId} Rejected";
                var body = $"Dear Customer,\\n\\nUnfortunately, your order #{orderId} has been rejected.\\n\\nPlease contact support for more information.";
                
                _logger.LogInformation($"Sending rejection email to {email} for order {orderId}");
                await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending rejection email to {email}");
                throw;
            }
        }

        private async Task SendEmailAsync(string email, string subject, string body)
        {
            // TODO: Integrate with real SMTP server
            // For now, just log the email
            _logger.LogInformation($"Email would be sent to: {email}\\nSubject: {subject}\\nBody: {body}");
            await Task.CompletedTask;
        }
    }
}
