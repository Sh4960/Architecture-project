using Microsoft.AspNetCore.Mvc;
using OrderService.BLL.Interfaces;
using OrderService.Models;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderBLLService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderBLLService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrderById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();

            return Ok(order);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<Order>>> GetUserOrders(int userId)
        {
            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }

        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var items = request.Items.Select(i => (i.ProductId, i.Quantity, i.Price)).ToList();
            var order = await _orderService.CreateOrderAsync(request.UserId, items);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }

        [HttpPut("{id}/confirm")]
        public async Task<ActionResult<Order>> ConfirmOrder(int id)
        {
            var order = await _orderService.ConfirmOrderAsync(id);
            return Ok(order);
        }

        [HttpPut("{id}/reject")]
        public async Task<ActionResult<Order>> RejectOrder(int id)
        {
            var order = await _orderService.RejectOrderAsync(id);
            return Ok(order);
        }
    }

    public class CreateOrderRequest
    {
        public int UserId { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
