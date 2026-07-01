using ProductCatalogService.BLL.Interfaces;
using ProductCatalogService.DAL.Interfaces;
using ProductCatalogService.Models;

namespace ProductCatalogService.BLL
{
    public class ProductBLLService : IProductBLLService
    {
        private readonly IProductDAL _productDAL;
        private readonly ILogger<ProductBLLService> _logger;

        public ProductBLLService(IProductDAL productDAL, ILogger<ProductBLLService> logger)
        {
            _productDAL = productDAL;
            _logger = logger;
        }

        public async Task<Product?> GetProductByIdAsync(string productId)
        {
            return await _productDAL.GetProductByIdAsync(productId);
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(string category)
        {
            _logger.LogInformation($"Fetching products in category: {category}");
            return await _productDAL.GetProductsByCategoryAsync(category);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            product.CreatedAt = DateTime.UtcNow;
            _logger.LogInformation($"Creating product: {product.Name}");
            return await _productDAL.CreateProductAsync(product);
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            product.CreatedAt = DateTime.UtcNow;
            _logger.LogInformation($"Updating product: {product.Id}");
            return await _productDAL.UpdateProductAsync(product);
        }

        public async Task DeleteProductAsync(string productId)
        {
            _logger.LogInformation($"Deleting product: {productId}");
            await _productDAL.DeleteProductAsync(productId);
        }
    }
}
