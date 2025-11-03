using Business.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.DataRepositories
{
    public interface ISupplierRepository
    {
        Task<SupplierBusinessModel?> GetByIdAsync(string id, string groupId);
        
        Task<SupplierBusinessModel?> GetByDocumentAsync(string document, string groupId);
        
        Task<SupplierBusinessModel?> GetByEmailAsync(string email, string groupId);

        Task<(IEnumerable<SupplierBusinessModel> Items, int TotalCount)> SearchAsync(
            string groupId,
            string? term,
            int page,
            int pageSize
        );

        Task<SupplierBusinessModel> CreateAsync(SupplierBusinessModel supplier);

        Task<SupplierBusinessModel?> UpdateAsync(string id, string groupId, SupplierBusinessModel supplier);

        Task<SupplierBusinessModel?> ActivateAsync(string id, string groupId);

        Task<SupplierBusinessModel?> DeactivateAsync(string id, string groupId);
    }
}