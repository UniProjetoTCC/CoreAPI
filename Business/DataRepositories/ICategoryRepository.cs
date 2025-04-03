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
        Task<CategoryBusinessModel> CreateCategoryAsync(CategoryBusinessModel category);
        Task<string> CreateCategoryAsync(string name, string groupId, string? description = null, bool active = true);
        Task<List<CategoryBusinessModel>> GetAllByGroupIdAsync(string groupId);
    }
}