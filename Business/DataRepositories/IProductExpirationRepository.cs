using Business.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Business.DataRepositories
{
    public interface IProductExpirationRepository
    {
        Task<ProductExpirationBusinessModel?> GetByIdAsync(string id, string groupId);
        
        Task<List<ProductExpirationBusinessModel>> GetAllByGroupIdAsync(string groupId);
        
        Task<List<ProductExpirationBusinessModel>> GetByProductIdAsync(string productId, string groupId);
        
        Task<List<ProductExpirationBusinessModel>> GetByStockIdAsync(string stockId, string groupId);
        
        Task<List<ProductExpirationBusinessModel>> GetExpiringInDaysAsync(string groupId, int days);
        
        Task<List<ProductExpirationBusinessModel>> GetExpiredAsync(string groupId);
        
        Task<ProductExpirationBusinessModel?> CreateAsync(
            string productId,
            string stockId,
            string groupId,
            DateTime expirationDate,
            string? location,
            bool isActive = true);
        
        Task<ProductExpirationBusinessModel?> UpdateAsync(
            string id,
            string groupId,
            DateTime expirationDate,
            string? location,
            bool isActive);
        
        Task<ProductExpirationBusinessModel?> DeleteAsync(string id, string groupId);
    }
}