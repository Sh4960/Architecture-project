namespace NotificationService.Messaging
{
    public class OrderFinalizedEvent
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
