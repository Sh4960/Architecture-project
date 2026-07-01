namespace InventoryService.Models
{
    /// <summary>
    /// Inventory model for PostgreSQL
    /// Tracks quantity of each product
    /// </summary>
    public class Inventory
    {
        public int Id { get; set; }
        public int ProductId { get; set; } // Reference to ProductCatalogService
        public int Quantity { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
