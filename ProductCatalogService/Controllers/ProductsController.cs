using Microsoft.AspNetCore.Mvc;
using ProductCatalogService.BLL.Interfaces;
using ProductCatalogService.Models;

namespace ProductCatalogService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductBLLService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductBLLService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            // שלב 5: הוספת ה-Header להוכחת Load Balancing
            AddContainerIdHeader();

            var product = await _productService.GetProductByIdAsync(id.ToString());
            if (product == null)
                return NotFound();

            return Ok(product);
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<List<Product>>> GetProductsByCategory(string category)
        {
            // שלב 5: הוספת ה-Header להוכחת Load Balancing
            AddContainerIdHeader();

            var products = await _productService.GetProductsByCategoryAsync(category);
            return Ok(products);
        }

        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductRequest request)
        {
            var product = new Product
            {
                Name = request.Name,
                Category = request.Category,
                Price = request.Price,
                ImageUrl = request.ImageUrl,
                DonorId = request.DonorId,
                Attributes = request.Attributes
            };

            var created = await _productService.CreateProductAsync(product);
            return CreatedAtAction(nameof(GetProductById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> UpdateProduct(string id, [FromBody] UpdateProductRequest request)
        {
            var product = new Product
            {
                Id = int.Parse(id),
                Name = request.Name,
                Category = request.Category,
                Price = request.Price,
                ImageUrl = request.ImageUrl,
                DonorId = request.DonorId,
                Attributes = request.Attributes
            };

            var updated = await _productService.UpdateProductAsync(product);
            return Ok(updated);
        }

        // מתודת עזר פרטית שמוסיפה את ה-Header לריצה הנוכחית
        private void AddContainerIdHeader()
        {
            var containerName = Environment.GetEnvironmentVariable("CONTAINER_NAME") ?? "Unknown-Replica";
            Response.Headers.Append("X-Container-Id", containerName);
            _logger.LogInformation($"Request handled by instance: {containerName}");
        }
    }

    public class CreateProductRequest
    {
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int DonorId { get; set; }
        public Dictionary<string, object>? Attributes { get; set; }
    }

    public class UpdateProductRequest
    {
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int DonorId { get; set; }
        public Dictionary<string, object>? Attributes { get; set; }
    }
}