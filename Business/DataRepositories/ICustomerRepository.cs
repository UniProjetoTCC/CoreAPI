using Business.Models;

namespace Business.DataRepositories
{
    public interface ICustomerRepository
    {        
        Task<CustomerBusinessModel?> GetByDocumentAsync(string document, string groupId);
        Task<CustomerBusinessModel?> GetByIdAsync(string id, string groupId);
        
        Task<(IEnumerable<CustomerBusinessModel> Items, int TotalCount)> SearchByNameAsync(
            string name,
            string groupId,
            int page,
            int pageSize
        );
        
        Task<(IEnumerable<CustomerBusinessModel> Items, int TotalCount)> SearchAsync(
            string? term,
            string groupId,
            int page,
            int pageSize
        );
        
        Task<CustomerBusinessModel> CreateCustomerAsync(
            string groupId,
            string name,
            string document,
            string? email,
            string? phone,
            string? address
        );
        
        Task<CustomerBusinessModel?> UpdateCustomerAsync(
            string id,
            string groupId,
            string name,
            string document,
            string? email,
            string? phone,
            string? address
        );
        
        Task<CustomerBusinessModel?> DeleteCustomerAsync(string id, string groupId);
        
        Task<CustomerBusinessModel?> ActivateCustomerAsync(string id, string groupId);
        
        Task<CustomerBusinessModel?> DeactivateCustomerAsync(string id, string groupId);
        
        Task<CustomerBusinessModel?> LinkToLoyaltyProgramAsync(string id, string groupId, string loyaltyProgramId);
        
        Task<CustomerBusinessModel?> UnlinkFromLoyaltyProgramAsync(string id, string groupId);
        
        Task<CustomerBusinessModel?> AddLoyaltyPointsAsync(string id, string groupId, float amount);
        
        Task<CustomerBusinessModel?> RemoveLoyaltyPointsAsync(string id, string groupId, float amount);
        
        Task<CustomerBusinessModel?> AddPointsAsync(string id, string groupId, decimal points, string description);
        
        Task<CustomerBusinessModel?> RemovePointsAsync(string id, string groupId, decimal points, string description);
        
        Task<CustomerBusinessModel?> RemoveLoyaltyPointsDirectAsync(string id, string groupId, int points);
        
        Task<int> GetLoyaltyPointsAsync(string id, string groupId);
        
        Task<(bool HasCustomersWithPoints, int CustomerCount)> HasCustomersWithPointsInLoyaltyProgramAsync(string loyaltyProgramId, string groupId);
        
        Task<int> UnlinkAllCustomersFromLoyaltyProgramAsync(string loyaltyProgramId, string groupId);
    }
}