using ProductCatalogService.Models;

namespace ProductCatalogService.DAL.Interfaces
{
    public interface IProductDAL
    {
        Task<Product?> GetProductByIdAsync(string productId);
        Task<List<Product>> GetProductsByCategoryAsync(string category);
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task DeleteProductAsync(string productId);
    }
}
