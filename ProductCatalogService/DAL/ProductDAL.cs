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
            var objectId = ObjectId.Parse(productId);
            return await _productsCollection
                .Find(p => p.Id == productId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(string category)
        {
            return await _productsCollection
                .Find(p => p.Category == category)
                .ToListAsync();
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            product.Id = ObjectId.GenerateNewId().ToString();
            product.CreatedAt = DateTime.UtcNow;
            
            _logger.LogInformation($"Creating product in MongoDB: {product.Name}");
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
            _logger.LogInformation($"Deleting product from MongoDB: {productId}");
            var result = await _productsCollection.DeleteOneAsync(p => p.Id == productId);

            if (result.DeletedCount == 0)
                throw new Exception($"Product {productId} not found");
        }
    }
}
