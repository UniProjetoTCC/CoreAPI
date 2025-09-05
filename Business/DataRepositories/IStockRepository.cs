using Business.Models;

namespace Business.DataRepositories
{
    public interface IStockRepository
    {
        Task<StockBusinessModel?> GetByProductIdAsync(string productId, string groupId);
        Task<StockBusinessModel?> AddStockAsync(string productId, string groupId, int quantity, string userId, string? reason = null);
        Task<StockBusinessModel?> UpdateStockAsync(string productId, string groupId, int quantity, string userId, string? reason = null);
        Task<bool> HasStockAsync(string productId, string groupId, int requiredQuantity);
        Task<StockBusinessModel?> DeductStockAsync(string productId, string groupId, int quantity, string userId, string? reason = null);
        Task<int> GetTotalStockAsync(string productId, string groupId);
        Task<List<StockBusinessModel>> GetLowStockProductsAsync(string groupId, int threshold);
    }
}