namespace ProductCatalogService.Models
{
    /// <summary>
    /// Product (Gift) model for MongoDB
    /// Polyglot Persistence: Stored as a document in MongoDB
    /// </summary>
    public class Product
    {
        public int Id { get; set; } // Numeric ID
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int DonorId { get; set; }
        public bool IsRaffled { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object>? Attributes { get; set; } // Dynamic attributes per category
    }
}
