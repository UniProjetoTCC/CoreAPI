using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Business.Utils;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;

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

            // Normalize category name and description to remove accents
            string normalizedName = StringUtils.RemoveDiacritics(name);
            string? normalizedDescription = description != null ? StringUtils.RemoveDiacritics(description) : null;

            var newCategory = new CategoryModel
            {
                GroupId = groupId,
                Name = normalizedName, // Store normalized name
                Description = normalizedDescription, // Store normalized description
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

            // Normalize category name and description to remove accents
            string normalizedName = StringUtils.RemoveDiacritics(name);
            string? normalizedDescription = description != null ? StringUtils.RemoveDiacritics(description) : null;

            var newCategory = new CategoryModel
            {
                GroupId = groupId,
                Name = normalizedName, // Store normalized name
                Description = normalizedDescription, // Store normalized description
                Active = active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Categories.AddAsync(newCategory);
            await _context.SaveChangesAsync();

            return _mapper.Map<CategoryBusinessModel>(newCategory);
        }

        public async Task<CategoryBusinessModel?> UpdateCategoryAsync(string id, string? name = null, string? description = null)
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
                // Normalize category name to remove accents
                string normalizedName = StringUtils.RemoveDiacritics(name);
                category.Name = normalizedName;
            }

            if (!string.IsNullOrEmpty(description))
            {
                // Normalize category description to remove accents
                string normalizedDescription = StringUtils.RemoveDiacritics(description);
                category.Description = normalizedDescription;
            }

            category.UpdatedAt = DateTime.UtcNow;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return _mapper.Map<CategoryBusinessModel>(category);
        }

        public async Task<CategoryBusinessModel?> DeleteCategoryAsync(string id, string groupId)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(groupId))
            {
                return null;
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (category == null)
            {
                return null;
            }

            _context.Categories.Remove(category);
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

        public async Task<(List<CategoryBusinessModel> Items, int TotalCount)> SearchByNameAsync(
            string name,
            string groupId,
            int page = 1,
            int pageSize = 20)
        {

            if (string.IsNullOrEmpty(groupId))
                return (new List<CategoryBusinessModel>(), 0);

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            // Normalize the input; empty name becomes empty string
            string normalizedName = StringUtils.RemoveDiacritics(name ?? "");

            // Build query: filter by group; optionally filter by name if provided
            var query = _context.Categories.Where(c => c.GroupId == groupId);
            if (!string.IsNullOrWhiteSpace(normalizedName))
            {
                query = query.Where(c =>
                    EF.Functions.ILike(c.Name, $"%{normalizedName}%") ||
                    (c.Description != null && EF.Functions.ILike(c.Description, $"%{normalizedName}%"))
                );
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination in memory-efficient way
            var categories = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (_mapper.Map<List<CategoryBusinessModel>>(categories), totalCount);
        }

        public async Task<CategoryBusinessModel?> ActivateAsync(string id, string groupId)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(groupId))
            {
                return null;
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (category == null)
            {
                return null;
            }

            // Set category as active
            category.Active = true;
            category.UpdatedAt = DateTime.UtcNow;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return _mapper.Map<CategoryBusinessModel>(category);
        }

        public async Task<CategoryBusinessModel?> DeactivateAsync(string id, string groupId)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(groupId))
            {
                return null;
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.GroupId == groupId);

            if (category == null)
            {
                return null;
            }

            // Set category as inactive
            category.Active = false;
            category.UpdatedAt = DateTime.UtcNow;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();

            return _mapper.Map<CategoryBusinessModel>(category);
        }
    }
}
