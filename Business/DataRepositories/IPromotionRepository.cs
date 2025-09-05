using Business.Models;
using System.Threading.Tasks;

namespace Business.DataRepositories
{
    public interface IPromotionRepository
    {
        // Basic CRUD operations
        Task<PromotionBusinessModel?> GetByIdAsync(string id, string groupId);
        Task<(List<PromotionBusinessModel> items, int totalCount)> SearchByNameAsync(string name, string groupId, int page, int pageSize);
        Task<PromotionBusinessModel?> CreatePromotionAsync(string name, string? description, decimal discountPercentage, DateTime startDate, DateTime endDate, string groupId, bool isActive = true);
        Task<PromotionBusinessModel?> UpdatePromotionAsync(string id, string groupId, string name, string? description, decimal discountPercentage, DateTime startDate, DateTime endDate);
        Task<PromotionBusinessModel?> DeletePromotionAsync(string id, string groupId);
        
        // Activate/Deactivate operations
        Task<PromotionBusinessModel?> ActivateAsync(string id, string groupId);
        Task<PromotionBusinessModel?> DeactivateAsync(string id, string groupId);
        Task<int> DeactivateExpiredPromotionsAsync(string groupId);
    }
}