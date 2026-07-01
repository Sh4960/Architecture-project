namespace NotificationService.Models
{
    /// <summary>
    /// Notification model - tracks sent notifications
    /// No database persistence required for this phase
    /// </summary>
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int OrderId { get; set; }
        public string Type { get; set; } = null!; // OrderConfirmed, OrderRejected
        public string Email { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsSent { get; set; } = false;
        public DateTime SentAt { get; set; }
    }
}
