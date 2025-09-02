using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class ProductExpirationRepository : IProductExpirationRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public ProductExpirationRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ProductExpirationBusinessModel?> GetByIdAsync(string id, string groupId)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(groupId))
                return null;

            var expiration = await _context.ProductExpirations
                .Include(pe => pe.Product)
                .Include(pe => pe.Stock)
                .FirstOrDefaultAsync(pe => pe.Id == id && pe.GroupId == groupId);

            return expiration != null ? _mapper.Map<ProductExpirationBusinessModel>(expiration) : null;
        }

        public async Task<List<ProductExpirationBusinessModel>> GetAllByGroupIdAsync(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
                return new List<ProductExpirationBusinessModel>();

            var expirations = await _context.ProductExpirations
                .Include(pe => pe.Product)
                .Include(pe => pe.Stock)
                .Where(pe => pe.GroupId == groupId)
                .ToListAsync();

            return _mapper.Map<List<ProductExpirationBusinessModel>>(expirations);
        }

        public async Task<List<ProductExpirationBusinessModel>> GetByProductIdAsync(string productId, string groupId)
        {
            if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(groupId))
                return new List<ProductExpirationBusinessModel>();

            var expirations = await _context.ProductExpirations
                .Include(pe => pe.Product)
                .Include(pe => pe.Stock)
                .Where(pe => pe.ProductId == productId && pe.GroupId == groupId)
                .ToListAsync();

            return _mapper.Map<List<ProductExpirationBusinessModel>>(expirations);
        }

        public async Task<List<ProductExpirationBusinessModel>> GetByStockIdAsync(string stockId, string groupId)
        {
            if (string.IsNullOrEmpty(stockId) || string.IsNullOrEmpty(groupId))
                return new List<ProductExpirationBusinessModel>();

            var expirations = await _context.ProductExpirations
                .Include(pe => pe.Product)
                .Include(pe => pe.Stock)
                .Where(pe => pe.StockId == stockId && pe.GroupId == groupId)
                .ToListAsync();

            return _mapper.Map<List<ProductExpirationBusinessModel>>(expirations);
        }

        public async Task<List<ProductExpirationBusinessModel>> GetExpiringInDaysAsync(string groupId, int days)
        {
            if (string.IsNullOrEmpty(groupId) || days < 0)
                return new List<ProductExpirationBusinessModel>();

            var today = DateTime.UtcNow.Date;
            var futureDate = today.AddDays(days);

            var expirations = await _context.ProductExpirations
                .Include(pe => pe.Product)
                .Include(pe => pe.Stock)
                .Where(pe => pe.GroupId == groupId && 
                             pe.IsActive && 
                             pe.ExpirationDate.Date >= today && 
                             pe.ExpirationDate.Date <= futureDate)
                .OrderBy(pe => pe.ExpirationDate)
                .ToListAsync();

            return _mapper.Map<List<ProductExpirationBusinessModel>>(expirations);
        }

        public async Task<List<ProductExpirationBusinessModel>> GetExpiredAsync(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
                return new List<ProductExpirationBusinessModel>();

            var today = DateTime.UtcNow.Date;

            var expirations = await _context.ProductExpirations
                .Include(pe => pe.Product)
                .Include(pe => pe.Stock)
                .Where(pe => pe.GroupId == groupId && 
                             pe.IsActive && 
                             pe.ExpirationDate.Date < today)
                .OrderBy(pe => pe.ExpirationDate)
                .ToListAsync();

            return _mapper.Map<List<ProductExpirationBusinessModel>>(expirations);
        }

        public async Task<ProductExpirationBusinessModel?> CreateAsync(
            string productId,
            string stockId,
            string groupId,
            DateTime expirationDate,
            string? location,
            bool isActive = true)
        {
            if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(stockId) || string.IsNullOrEmpty(groupId))
                return null;

            // Verify that product and stock exist and belong to the group
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.GroupId == groupId);
            if (product == null)
                return null;

            var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.Id == stockId && s.GroupId == groupId);
            if (stock == null)
                return null;

            var expiration = new ProductExpirationModel
            {
                ProductId = productId,
                StockId = stockId,
                GroupId = groupId,
                ExpirationDate = expirationDate,
                Location = location,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ProductExpirations.AddAsync(expiration);
            await _context.SaveChangesAsync();

            return _mapper.Map<ProductExpirationBusinessModel>(expiration);
        }

        public async Task<ProductExpirationBusinessModel?> UpdateAsync(
            string id,
            string groupId,
            DateTime expirationDate,
            string? location,
            bool isActive)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(groupId))
                return null;

            var expiration = await _context.ProductExpirations
                .FirstOrDefaultAsync(pe => pe.Id == id && pe.GroupId == groupId);

            if (expiration == null)
                return null;

            expiration.ExpirationDate = expirationDate;
            expiration.Location = location;
            expiration.IsActive = isActive;
            expiration.UpdatedAt = DateTime.UtcNow;

            _context.ProductExpirations.Update(expiration);
            await _context.SaveChangesAsync();

            return _mapper.Map<ProductExpirationBusinessModel>(expiration);
        }

        public async Task<ProductExpirationBusinessModel?> DeleteAsync(string id, string groupId)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(groupId))
                return null;

            var expiration = await _context.ProductExpirations
                .FirstOrDefaultAsync(pe => pe.Id == id && pe.GroupId == groupId);

            if (expiration == null)
                return null;

            _context.ProductExpirations.Remove(expiration);
            await _context.SaveChangesAsync();

            return _mapper.Map<ProductExpirationBusinessModel>(expiration);
        }
    }
}
