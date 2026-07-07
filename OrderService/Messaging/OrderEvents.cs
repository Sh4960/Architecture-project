namespace OrderService.Messaging
{
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderPlacedEvent
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class InventoryReservedEvent
    {
        public int OrderId { get; set; }
    }

    public class InventoryRejectedEvent
    {
        public int OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class OrderFinalizedEvent
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
