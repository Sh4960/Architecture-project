using InventoryService.Models;

namespace InventoryService.BLL.Interfaces
{
    public interface IInventoryBLLService
    {
        Task<Inventory?> GetInventoryByProductIdAsync(int productId);
        Task<bool> ReserveAsync(int productId, int quantity);
        Task<bool> ReleaseAsync(int productId, int quantity);
        Task<bool> IsAvailableAsync(int productId, int quantity);
    }
}
