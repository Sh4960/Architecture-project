using ProductCatalogService.BLL.Interfaces;
using ProductCatalogService.DAL.Interfaces;
using ProductCatalogService.Models;

namespace ProductCatalogService.BLL
{
    public class ProductBLLService : IProductBLLService
    {
        private readonly IProductDAL _productDAL;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ProductBLLService> _logger;
        private const string PRODUCT_CACHE_PREFIX = "product:";
        private const string CATEGORY_CACHE_PREFIX = "category:";
        private const int CACHE_DURATION_MINUTES = 60;

        public ProductBLLService(IProductDAL productDAL, ICacheService cacheService, ILogger<ProductBLLService> logger)
        {
            _productDAL = productDAL;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Product?> GetProductByIdAsync(string productId)
        {
            // Cache-Aside Pattern (Read)
            string cacheKey = $"{PRODUCT_CACHE_PREFIX}{productId}";
            
            // Try to get from cache first
            var cachedProduct = await _cacheService.GetAsync<Product>(cacheKey);
            if (cachedProduct != null)
            {
                _logger.LogInformation("Retrieved product {ProductId} from cache", productId);
                return cachedProduct;
            }

            // Cache miss - fetch from database
            _logger.LogInformation("Cache miss for product {ProductId}, fetching from database", productId);
            var product = await _productDAL.GetProductByIdAsync(productId);
            
            // Store in cache for future requests
            if (product != null)
            {
                await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                _logger.LogInformation("Cached product {ProductId} for {DurationMinutes} minutes", productId, CACHE_DURATION_MINUTES);
            }

            return product;
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(string category)
        {
            // Cache-Aside Pattern (Read)
            string cacheKey = $"{CATEGORY_CACHE_PREFIX}{category}";

            var cachedProducts = await _cacheService.GetAsync<List<Product>>(cacheKey);
            if (cachedProducts != null)
            {
                _logger.LogInformation("Retrieved {Count} products in category {Category} from cache", cachedProducts.Count, category);
                return cachedProducts;
            }

            // Cache miss - fetch from database
            _logger.LogInformation("Cache miss for category {Category}, fetching from database", category);
            var products = await _productDAL.GetProductsByCategoryAsync(category);
            
            // Store in cache
            await _cacheService.SetAsync(cacheKey, products, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            _logger.LogInformation("Cached {Count} products in category {Category} for {DurationMinutes} minutes", products.Count, category, CACHE_DURATION_MINUTES);

            return products;
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            product.CreatedAt = DateTime.UtcNow;
            _logger.LogInformation("Creating product: {Name}", product.Name);
            var created = await _productDAL.CreateProductAsync(product);
            
            // Store newly created product in cache
            await _cacheService.SetAsync($"{PRODUCT_CACHE_PREFIX}{created.Id}", created, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            _logger.LogInformation("Cached newly created product {ProductId}", created.Id);
            
            // Invalidate category cache (since a new product was added)
            if (!string.IsNullOrEmpty(created.Category))
            {
                await _cacheService.InvalidateAsync($"{CATEGORY_CACHE_PREFIX}{created.Category}");
                _logger.LogInformation("Invalidated category cache for {Category} after creating new product", created.Category);
            }

            return created;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            product.CreatedAt = DateTime.UtcNow;
            _logger.LogInformation("Updating product: {ProductId}", product.Id);
            var updated = await _productDAL.UpdateProductAsync(product);
            
            // Invalidation Strategy: Invalidate both product and category cache
            await _cacheService.InvalidateAsync($"{PRODUCT_CACHE_PREFIX}{product.Id}");
            _logger.LogInformation("Invalidated cache for updated product {ProductId}", product.Id);
            
            if (!string.IsNullOrEmpty(updated.Category))
            {
                await _cacheService.InvalidateAsync($"{CATEGORY_CACHE_PREFIX}{updated.Category}");
                _logger.LogInformation("Invalidated category cache for {Category} after updating product", updated.Category);
            }

            return updated;
        }

        public async Task DeleteProductAsync(string productId)
        {
            _logger.LogInformation("Deleting product: {ProductId}", productId);
            
            // Get product first to know its category
            var product = await _productDAL.GetProductByIdAsync(productId);
            
            await _productDAL.DeleteProductAsync(productId);
            
            // Invalidate caches
            await _cacheService.InvalidateAsync($"{PRODUCT_CACHE_PREFIX}{productId}");
            _logger.LogInformation("Invalidated cache for deleted product {ProductId}", productId);
            
            if (product != null && !string.IsNullOrEmpty(product.Category))
            {
                await _cacheService.InvalidateAsync($"{CATEGORY_CACHE_PREFIX}{product.Category}");
                _logger.LogInformation("Invalidated category cache for {Category} after deleting product", product.Category);
            }
        }
    }
}
