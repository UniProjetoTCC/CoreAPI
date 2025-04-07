using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Data.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public CategoryRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<CategoryBusinessModel?> GetByIdAsync(string id, string groupId)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            return category != null ? _mapper.Map<CategoryBusinessModel>(category) : null;
        }

        public async Task<CategoryBusinessModel?> GetByNameAsync(string name, string groupId)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower() && c.GroupId == groupId);

            return category != null ? _mapper.Map<CategoryBusinessModel>(category) : null;
        }

        public async Task<string?> CreateCategoryGetIdAsync(string name, string groupId, string? description = null, bool active = true)
        {

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(groupId))
            {
                return null;
            }

            var newCategory = new CategoryModel
            {
                GroupId = groupId,
                Name = name,
                Description = description,
                Active = active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Categories.AddAsync(newCategory);
            await _context.SaveChangesAsync();

            return newCategory.Id;
        }

        public async Task<CategoryBusinessModel?> CreateCategoryAsync(string name, string groupId, string? description = null, bool active = true)
        {
            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(name))
            {
                return null;
            }

            var newCategory = new CategoryModel
            {
                GroupId = groupId,
                Name = name,
                Description = description,
                Active = active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Categories.AddAsync(newCategory);
            await _context.SaveChangesAsync();

            return _mapper.Map<CategoryBusinessModel>(newCategory);
        }

        public async Task<CategoryBusinessModel?> UpdateCategoryAsync(string id, string? name = null, string? description = null, bool? active = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null; 
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                throw new Exception("Category not found"); 
            }

            // Atualizar apenas os campos fornecidos
            if (!string.IsNullOrEmpty(name))
            {
                category.Name = name;
            }

            if (!string.IsNullOrEmpty(description))
            {
                category.Description = description;
            }

            if (active.HasValue)
            {
                category.Active = active.Value;
            }

            category.UpdatedAt = DateTime.UtcNow;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return _mapper.Map<CategoryBusinessModel>(category);
        }

        public async Task<List<CategoryBusinessModel>> GetAllByGroupIdAsync(string groupId)
        {
            var categories = await _context.Categories
                .Where(c => c.GroupId == groupId && c.Active)
                .ToListAsync();

            return _mapper.Map<List<CategoryBusinessModel>>(categories);
        }
    }
}
