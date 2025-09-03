using Business.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.DataRepositories
{
    public interface ILoyaltyProgramRepository
    {
        Task<List<LoyaltyProgramBusinessModel>> GetAllAsync(string groupId);
        
        Task<LoyaltyProgramBusinessModel?> GetByIdAsync(string id, string groupId);
        
        Task<List<LoyaltyProgramBusinessModel>> GetActiveAsync(string groupId);
        
        Task<LoyaltyProgramBusinessModel?> CreateAsync(
            string name,
            string description,
            decimal centsToPoints,
            decimal pointsToCents,
            decimal discountPercentage,
            string groupId,
            bool isActive = true);
        
        Task<LoyaltyProgramBusinessModel?> UpdateAsync(
            string id,
            string groupId,
            string name,
            string description,
            decimal centsToPoints,
            decimal pointsToCents,
            decimal discountPercentage);
        
        Task<LoyaltyProgramBusinessModel?> ActivateAsync(string id, string groupId);
        
        Task<LoyaltyProgramBusinessModel?> DeactivateAsync(string id, string groupId);
        
        Task<LoyaltyProgramBusinessModel?> DeleteAsync(string id, string groupId);
    }
}