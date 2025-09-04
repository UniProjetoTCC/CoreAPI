
using Business.Models;

namespace Business.DataRepositories
{
    public interface ISupplierRepository
    {
        Task<SupplierBusinessModel?> GetByIdAsync(string id, string groupId);
        Task<SupplierBusinessModel?> GetByDocumentAsync(string document, string groupId);
        Task<SupplierBusinessModel> CreateAsync(SupplierBusinessModel supplier);
        Task<SupplierBusinessModel?> UpdateAsync(SupplierBusinessModel supplier);
        Task<bool> DeleteAsync(string id, string groupId);
        Task<(List<SupplierBusinessModel> Items, int TotalCount)> SearchAsync(string groupId, string? name, int page, int pageSize);
    }
}