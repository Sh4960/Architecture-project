using StackExchange.Redis;
using System.Text.Json;
using InventoryService.DAL.Interfaces;
using InventoryService.Models;

namespace InventoryService.DAL
{
    public class InventoryDAL : IInventoryDAL
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private const string INVENTORY_KEY_PREFIX = "inventory:";

        public InventoryDAL(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = redis.GetDatabase();
            
            // Initialize seed data if not exists
            InitializeSeedData();
        }

        private void InitializeSeedData()
        {
            try
            {
                var existingKey = _db.KeyExists($"{INVENTORY_KEY_PREFIX}1");
                if (!existingKey)
                {
                    var seedInventory = new Inventory { Id = 1, ProductId = 1, Quantity = 100, LastUpdated = DateTime.UtcNow };
                    var json = JsonSerializer.Serialize(seedInventory);
                    _db.StringSet($"{INVENTORY_KEY_PREFIX}1", json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing seed data: {ex.Message}");
            }
        }

        public async Task<Inventory?> GetInventoryByProductIdAsync(int productId)
        {
            try
            {
                var key = $"{INVENTORY_KEY_PREFIX}{productId}";
                var value = await _db.StringGetAsync(key);
                
                if (value.IsNullOrEmpty)
                    return null;

                return JsonSerializer.Deserialize<Inventory>(value.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting inventory: {ex.Message}");
                return null;
            }
        }

        public async Task<Inventory> UpdateInventoryAsync(int productId, int quantityChange)
        {
            try
            {
                var inventory = await GetInventoryByProductIdAsync(productId);
                
                if (inventory == null)
                {
                    inventory = new Inventory { Id = productId, ProductId = productId, Quantity = quantityChange, LastUpdated = DateTime.UtcNow };
                }
                else
                {
                    inventory.Quantity += quantityChange;
                    inventory.LastUpdated = DateTime.UtcNow;
                }

                var key = $"{INVENTORY_KEY_PREFIX}{productId}";
                var json = JsonSerializer.Serialize(inventory);
                await _db.StringSetAsync(key, json);

                return inventory;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating inventory: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ReserveInventoryAsync(int productId, int quantity)
        {
            try
            {
                var inventory = await GetInventoryByProductIdAsync(productId);

                if (inventory == null || inventory.Quantity < quantity)
                    return false;

                inventory.Quantity -= quantity;
                inventory.LastUpdated = DateTime.UtcNow;

                var key = $"{INVENTORY_KEY_PREFIX}{productId}";
                var json = JsonSerializer.Serialize(inventory);
                await _db.StringSetAsync(key, json);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reserving inventory: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ReleaseInventoryAsync(int productId, int quantity)
        {
            try
            {
                var inventory = await GetInventoryByProductIdAsync(productId);

                if (inventory == null)
                    return false;

                inventory.Quantity += quantity;
                inventory.LastUpdated = DateTime.UtcNow;

                var key = $"{INVENTORY_KEY_PREFIX}{productId}";
                var json = JsonSerializer.Serialize(inventory);
                await _db.StringSetAsync(key, json);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error releasing inventory: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsAvailableAsync(int productId, int quantity)
        {
            try
            {
                var inventory = await GetInventoryByProductIdAsync(productId);
                return inventory != null && inventory.Quantity >= quantity;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking availability: {ex.Message}");
                return false;
            }
        }
    }
}
 