using InventoryService.BLL.Interfaces;
using InventoryService.DAL.Interfaces;
using InventoryService.Models;

namespace InventoryService.BLL
{
    public class InventoryBLLService : IInventoryBLLService
    {
        private readonly IInventoryDAL _inventoryDAL;
        private readonly ILogger<InventoryBLLService> _logger;

        public InventoryBLLService(IInventoryDAL inventoryDAL, ILogger<InventoryBLLService> logger)
        {
            _inventoryDAL = inventoryDAL;
            _logger = logger;
        }

        public async Task<Inventory?> GetInventoryByProductIdAsync(int productId)
        {
            return await _inventoryDAL.GetInventoryByProductIdAsync(productId);
        }

        public async Task<bool> ReserveAsync(int productId, int quantity)
        {
            _logger.LogInformation($"Attempting to reserve {quantity} units of product {productId}");
            return await _inventoryDAL.ReserveInventoryAsync(productId, quantity);
        }

        public async Task<bool> ReleaseAsync(int productId, int quantity)
        {
            _logger.LogInformation($"Releasing {quantity} units of product {productId}");
            return await _inventoryDAL.ReleaseInventoryAsync(productId, quantity);
        }

        public async Task<bool> IsAvailableAsync(int productId, int quantity)
        {
            var inventory = await _inventoryDAL.GetInventoryByProductIdAsync(productId);
            return inventory != null && inventory.Quantity >= quantity;
        }
    }
}
