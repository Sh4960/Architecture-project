using Microsoft.AspNetCore.Mvc;
using InventoryService.BLL.Interfaces;

namespace InventoryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoriesController : ControllerBase
    {
        private readonly IInventoryBLLService _inventoryService;
        private readonly ILogger<InventoriesController> _logger;

        public InventoriesController(IInventoryBLLService inventoryService, ILogger<InventoriesController> logger)
        {
            _inventoryService = inventoryService;
            _logger = logger;
        }

        [HttpGet("{productId}")]
        public async Task<ActionResult> GetInventory(int productId)
        {
            var inventory = await _inventoryService.GetInventoryByProductIdAsync(productId);
            if (inventory == null)
                return NotFound();

            return Ok(inventory);
        }

        [HttpPost("reserve")]
        public async Task<ActionResult<bool>> ReserveInventory([FromBody] ReserveInventoryRequest request)
        {
            var result = await _inventoryService.ReserveAsync(request.ProductId, request.Quantity);
            if (!result)
                return BadRequest("Insufficient inventory");

            return Ok(true);
        }

        [HttpPost("release")]
        public async Task<ActionResult<bool>> ReleaseInventory([FromBody] ReleaseInventoryRequest request)
        {
            var result = await _inventoryService.ReleaseAsync(request.ProductId, request.Quantity);
            return Ok(result);
        }

        [HttpGet("check/{productId}/{quantity}")]
        public async Task<ActionResult<bool>> CheckAvailability(int productId, int quantity)
        {
            var available = await _inventoryService.IsAvailableAsync(productId, quantity);
            return Ok(available);
        }
    }

    public class ReserveInventoryRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class ReleaseInventoryRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
