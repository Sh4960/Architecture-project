using InventoryService.Models;

namespace InventoryService.DAL.Interfaces
{
    public interface IInventoryDAL
    {
        Task<Inventory?> GetInventoryByProductIdAsync(int productId);
        Task<Inventory> UpdateInventoryAsync(int productId, int quantityChange);
        Task<bool> ReserveInventoryAsync(int productId, int quantity);
        Task<bool> ReleaseInventoryAsync(int productId, int quantity);
    }
}
