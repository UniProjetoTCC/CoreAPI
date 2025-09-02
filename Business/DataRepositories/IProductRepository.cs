using Business.Models;

namespace Business.DataRepositories
{
    public interface IProductRepository
    {
        Task<ProductBusinessModel?> GetById(string id, string groupId);

        Task<(List<ProductBusinessModel> Items, int TotalCount)> SearchByNameAsync(
            string name,
            string groupId,
            int page = 1,
            int pageSize = 20
        );

        Task<ProductBusinessModel?> CreateProductAsync(
            string groupId,
            string categoryId,
            string name,
            string sku,
            string barCode,
            string? description,
            decimal price,
            decimal cost,
            bool active = true
        );

        Task<ProductBusinessModel?> UpdateProductAsync(
            string id,
            string groupId,
            string categoryId,
            string name,
            string sku,
            string barCode,
            string? description,
            decimal cost,
            bool active
        );

        Task<ProductBusinessModel?> DeleteProductAsync(
            string id,
            string groupId
        );

        Task<bool> HasProductsInCategoryAsync(
            string categoryId,
            string groupId
        );

        Task<List<ProductBusinessModel>> GetProductsByBarCodeAsync(
            string barCode,
            string groupId
        );

        Task<List<ProductBusinessModel>> GetProductsBySKUAsync(
            string sku,
            string groupId
        );

        Task<(bool IsBarcodeDuplicate, bool IsSKUDuplicate, ProductBusinessModel? Product)> CheckDuplicateProductAsync(
            string barCode,
            string sku,
            string groupId
        );

        Task<ProductBusinessModel?> UpdateProductPriceAsync(
            string id,
            string groupId,
            decimal newPrice,
            string userId,
            string? reason = null
        );
    }
}