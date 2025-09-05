using Business.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Business.DataRepositories
{
    public interface IProductPromotionRepository
    {
        // Product-Promotion management
        Task<List<ProductBusinessModel>> GetProductsInPromotionAsync(string promotionId, string groupId);
        Task<ProductPromotionBusinessModel?> AddProductToPromotionAsync(string productId, string promotionId, string groupId);
        Task<bool> RemoveProductFromPromotionAsync(string productId, string promotionId, string groupId);
        Task<int> AddProductsToPromotionAsync(List<string> productIds, string promotionId, string groupId);
        Task<int> RemoveProductsFromPromotionAsync(List<string> productIds, string promotionId, string groupId);
        Task<int> RemoveAllProductsFromPromotionAsync(string promotionId, string groupId);
        Task<List<PromotionBusinessModel>> GetPromotionsByProductIdAsync(string productId, string groupId);
    }
}
