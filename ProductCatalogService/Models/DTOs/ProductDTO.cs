namespace ProductCatalogService.Models.DTOs
{
    public class ProductDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int DonorId { get; set; }
        public bool IsRaffled { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object>? Attributes { get; set; }
    }

    public class CreateProductDTO
    {
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int DonorId { get; set; }
        public Dictionary<string, object>? Attributes { get; set; }
    }

    public class UpdateProductDTO
    {
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int DonorId { get; set; }
        public Dictionary<string, object>? Attributes { get; set; }
    }
}
