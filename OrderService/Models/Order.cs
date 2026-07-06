using System.Text.Json.Serialization;

namespace OrderService.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Rejected
        public decimal TotalPrice { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; } // From ProductCatalogService
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        [JsonIgnore]
        public Order Order { get; set; } = null!;
    }
}
