using Business.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.DataRepositories
{
    public interface IPaymentMethodRepository
    {
        Task<List<PaymentMethodBusinessModel>> GetAllAsync(string groupId);
        
        Task<PaymentMethodBusinessModel?> GetByIdAsync(string id, string groupId);
        
        Task<List<PaymentMethodBusinessModel>> GetActiveAsync(string groupId);
        
        Task<PaymentMethodBusinessModel?> CreateAsync(
            string name,
            string code,
            string description,
            string groupId,
            bool isActive = true);
        
        Task<PaymentMethodBusinessModel?> UpdateAsync(
            string id,
            string groupId,
            string name,
            string code,
            string description);
        
        Task<PaymentMethodBusinessModel?> ActivateAsync(string id, string groupId);
        
        Task<PaymentMethodBusinessModel?> DeactivateAsync(string id, string groupId);
        
        Task<PaymentMethodBusinessModel?> DeleteAsync(string id, string groupId);
    }
}