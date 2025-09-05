using Business.Models;

namespace Business.DataRepositories
{
    public interface ICategoryRepository
    {
        Task<CategoryBusinessModel?> GetByIdAsync(string id, string groupId);
        Task<CategoryBusinessModel?> GetByNameAsync(string name, string groupId);
        Task<string?> CreateCategoryGetIdAsync(string name, string groupId, string? description = null, bool active = true);
        Task<List<CategoryBusinessModel>> GetAllByGroupIdAsync(string groupId);
        Task<CategoryBusinessModel?> CreateCategoryAsync(string name, string groupId, string? description = null, bool active = true);
        Task<CategoryBusinessModel?> UpdateCategoryAsync(string id, string name, string? description = null);
        Task<CategoryBusinessModel?> DeleteCategoryAsync(string id, string groupId);
        Task<(List<CategoryBusinessModel> Items, int TotalCount)> SearchByNameAsync(string name, string groupId, int page = 1, int pageSize = 20);
        Task<CategoryBusinessModel?> ActivateAsync(string id, string groupId);
        Task<CategoryBusinessModel?> DeactivateAsync(string id, string groupId);
    }
}