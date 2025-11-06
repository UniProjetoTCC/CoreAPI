using Business.Models;
using System.Threading.Tasks;

namespace Business.Services.Base
{
    public interface IPurchaseOrderService
    {
        Task<PurchaseOrderBusinessModel?> CreateOrderAsync(PurchaseOrderBusinessModel order, string userId);
        Task<PurchaseOrderBusinessModel?> GetOrderByIdAsync(string id, string groupId);
        Task<PurchaseOrderBusinessModel?> CompleteOrderAsync(string id, string groupId, string userId);
        Task<PurchaseOrderBusinessModel?> CancelOrderAsync(string id, string groupId, string userId);
    }
}