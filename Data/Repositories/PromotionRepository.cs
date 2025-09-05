using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace Data.Repositories
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public PromotionRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PromotionBusinessModel?> GetByIdAsync(string id, string groupId)
        {
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);
                
            return promotion == null ? null : _mapper.Map<PromotionBusinessModel>(promotion);
        }

        public async Task<(List<PromotionBusinessModel> items, int totalCount)> SearchByNameAsync(string name, string groupId, int page, int pageSize)
        {
            var query = _context.Promotions
                .Where(p => p.GroupId == groupId);

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(p => p.Name.Contains(name));
            }

            var totalCount = await query.CountAsync();
            var promotions = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (_mapper.Map<List<PromotionBusinessModel>>(promotions), totalCount);
        }

        public async Task<PromotionBusinessModel?> CreatePromotionAsync(string name, string? description, decimal discountPercentage, DateTime startDate, DateTime endDate, string groupId, bool isActive = true)
        {
            var promotion = new PromotionModel
            {
                Name = name,
                Description = description,
                DiscountPercentage = discountPercentage,
                StartDate = startDate,
                EndDate = endDate,
                GroupId = groupId,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

            return _mapper.Map<PromotionBusinessModel>(promotion);
        }

        public async Task<PromotionBusinessModel?> UpdatePromotionAsync(string id, string groupId, string name, string? description, decimal discountPercentage, DateTime startDate, DateTime endDate)
        {
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (promotion == null)
            {
                return null;
            }

            promotion.Name = name;
            promotion.Description = description;
            promotion.DiscountPercentage = discountPercentage;
            promotion.StartDate = startDate;
            promotion.EndDate = endDate;
            promotion.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<PromotionBusinessModel>(promotion);
        }

        public async Task<PromotionBusinessModel?> DeletePromotionAsync(string id, string groupId)
        {
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (promotion == null)
            {
                return null;
            }

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();

            return _mapper.Map<PromotionBusinessModel>(promotion);
        }

        public async Task<PromotionBusinessModel?> ActivateAsync(string id, string groupId)
        {
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (promotion == null)
            {
                return null;
            }

            promotion.IsActive = true;
            promotion.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<PromotionBusinessModel>(promotion);
        }

        public async Task<PromotionBusinessModel?> DeactivateAsync(string id, string groupId)
        {
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (promotion == null)
            {
                return null;
            }

            promotion.IsActive = false;
            promotion.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<PromotionBusinessModel>(promotion);
        }

        public async Task<int> DeactivateExpiredPromotionsAsync(string groupId)
        {
            var expiredPromotions = await _context.Promotions
                .Where(p => p.GroupId == groupId && p.IsActive && p.EndDate < DateTime.UtcNow)
                .ToListAsync();

            if (!expiredPromotions.Any())
            {
                return 0;
            }

            foreach (var promotion in expiredPromotions)
            {
                promotion.IsActive = false;
            }

            return await _context.SaveChangesAsync();
        }
    }
}
