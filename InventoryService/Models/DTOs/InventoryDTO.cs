namespace InventoryService.Models.DTOs
{
    public class InventoryDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class InventoryResponseDTO
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public bool Available { get; set; }
    }
}
