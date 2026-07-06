using MongoDB.Bson;
using MongoDB.Driver;
using ProductCatalogService.DAL.Interfaces;
using ProductCatalogService.Models;

namespace ProductCatalogService.DAL
{
    public class ProductDAL : IProductDAL
    {
        private readonly IMongoCollection<Product> _productsCollection;
        private readonly ILogger<ProductDAL> _logger;

        public ProductDAL(IMongoClient mongoClient, ILogger<ProductDAL> logger)
        {
            _logger = logger;
            var database = mongoClient.GetDatabase("ProductCatalogDB");
            _productsCollection = database.GetCollection<Product>("Products");
        }

        public async Task<Product?> GetProductByIdAsync(string productId)
        {
            try
            {
                if (int.TryParse(productId, out int id))
                {
                    return await _productsCollection
                        .Find(p => p.Id == id)
                        .FirstOrDefaultAsync();
                }
                _logger.LogWarning($"Invalid product id format: {productId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting product by id {productId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(string category)
        {
            return await _productsCollection
                .Find(p => p.Category == category)
                .ToListAsync();
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            // Generate next numeric ID
            var maxIdProduct = await _productsCollection
                .Find(p => true)
                .Sort(Builders<Product>.Sort.Descending(p => p.Id))
                .FirstOrDefaultAsync();
            
            product.Id = (maxIdProduct?.Id ?? 0) + 1;
            product.CreatedAt = DateTime.UtcNow;
            
            _logger.LogInformation($"Creating product in MongoDB: {product.Name} with ID {product.Id}");
            await _productsCollection.InsertOneAsync(product);
            return product;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            _logger.LogInformation($"Updating product in MongoDB: {product.Id}");
            var result = await _productsCollection.ReplaceOneAsync(
                p => p.Id == product.Id,
                product,
                new ReplaceOptions { IsUpsert = false }
            );

            if (result.MatchedCount == 0)
                throw new Exception($"Product {product.Id} not found");

            return product;
        }

        public async Task DeleteProductAsync(string productId)
        {
            if (int.TryParse(productId, out int id))
            {
                _logger.LogInformation($"Deleting product from MongoDB: {id}");
                var result = await _productsCollection.DeleteOneAsync(p => p.Id == id);

                if (result.DeletedCount == 0)
                    throw new Exception($"Product {id} not found");
            }
            else
            {
                throw new Exception($"Invalid product id format: {productId}");
            }
        }
    }
}
