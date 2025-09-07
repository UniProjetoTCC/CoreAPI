using Business.Models;

namespace Business.DataRepositories
{
    public interface IStockMovementRepository
    {
        Task<StockMovementBusinessModel?> GetByIdAsync(string id);
        Task<List<StockMovementBusinessModel>> GetByStockIdAsync(string stockId, int page = 1, int pageSize = 20);
        Task<List<StockMovementBusinessModel>> GetByProductIdAsync(string productId, string groupId, int page = 1, int pageSize = 20);
        Task<StockMovementBusinessModel> AddMovementAsync(StockMovementBusinessModel movement);
        Task<int> GetTotalMovementsForProductAsync(string productId, string groupId);
        Task<List<StockMovementBusinessModel>> GetAllByDateRangeAndGroupIdAsync(DateTime startDate, DateTime endDate, string groupId);
    }
}