using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiProject.Data;
using WebApiProject.Models;

namespace WebApiProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _db;

        public InventoryController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/inventory/{giftId}
        [HttpGet("{giftId}")]
        public async Task<IActionResult> GetByGift(int giftId)
        {
            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.GiftId == giftId);
            if (inv == null) return NotFound();
            return Ok(inv);
        }

        // POST: api/inventory
        // Create or update inventory record
        [HttpPost]
        //[Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateOrUpdate([FromBody] Inventory model)
        {
            if (model == null) return BadRequest();

            var exists = await _db.Inventories.FirstOrDefaultAsync(i => i.GiftId == model.GiftId);
            if (exists == null)
            {
                _db.Inventories.Add(model);
            }
            else
            {
                exists.Quantity = model.Quantity;
                _db.Inventories.Update(exists);
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        // POST: api/inventory/{giftId}/reserve
        // Body: { "quantity": 2 }
        [HttpPost("{giftId}/reserve")]
        public async Task<IActionResult> Reserve(int giftId, [FromBody] ReserveRequest req)
        {
            if (req == null || req.Quantity <= 0) return BadRequest("Invalid quantity");

            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.GiftId == giftId);
            if (inv == null) return NotFound("Inventory record not found");

            if (inv.Quantity < req.Quantity)
                return BadRequest("Not enough inventory");

            inv.Quantity -= req.Quantity;
            _db.Inventories.Update(inv);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, remaining = inv.Quantity });
        }

        public class ReserveRequest
        {
            public int Quantity { get; set; }
        }
    }
}
