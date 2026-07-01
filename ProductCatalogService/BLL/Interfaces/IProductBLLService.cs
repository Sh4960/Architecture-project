using ProductCatalogService.Models;

namespace ProductCatalogService.BLL.Interfaces
{
    public interface IProductBLLService
    {
        Task<Product?> GetProductByIdAsync(string productId);
        Task<List<Product>> GetProductsByCategoryAsync(string category);
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task DeleteProductAsync(string productId);
    }
}
