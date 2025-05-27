using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        Task<CategoryBusinessModel?> UpdateCategoryAsync(string id, string name, string? description = null, bool? active = null);
        Task<CategoryBusinessModel?> DeleteCategoryAsync(string id, string groupId);
        Task<(List<CategoryBusinessModel> Items, int TotalCount)> SearchByNameAsync(string name, string groupId, int page = 1, int pageSize = 20);
    }
}