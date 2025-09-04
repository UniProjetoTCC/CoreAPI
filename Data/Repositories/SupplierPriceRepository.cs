using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class SupplierPriceRepository : ISupplierPriceRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public SupplierPriceRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<SupplierPriceBusinessModel> CreateAsync(SupplierPriceBusinessModel price)
        {
            var model = _mapper.Map<SupplierPriceModel>(price);
            await _context.SupplierPrices.AddAsync(model);
            await _context.SaveChangesAsync();
            return _mapper.Map<SupplierPriceBusinessModel>(model);
        }

        public async Task<SupplierPriceBusinessModel?> UpdateAsync(SupplierPriceBusinessModel price)
        {
            var model = await _context.SupplierPrices.FirstOrDefaultAsync(p => p.Id == price.Id && p.GroupId == price.GroupId);
            if (model == null) return null;

            _mapper.Map(price, model);
            model.UpdatedAt = DateTime.UtcNow;

            _context.SupplierPrices.Update(model);
            await _context.SaveChangesAsync();

            return _mapper.Map<SupplierPriceBusinessModel>(model);
        }

        public async Task<bool> DeleteAsync(string id, string groupId)
        {
            var model = await _context.SupplierPrices.FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);
            if (model == null) return false;

            _context.SupplierPrices.Remove(model);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DoesPriceExistForProductAsync(string supplierId, string productId, string groupId)
        {
            return await _context.SupplierPrices.AnyAsync(p => p.SupplierId == supplierId && p.ProductId == productId && p.GroupId == groupId);
        }

        public async Task<SupplierPriceBusinessModel?> GetByIdAsync(string id, string groupId)
        {
            var model = await _context.SupplierPrices
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);
            return _mapper.Map<SupplierPriceBusinessModel>(model);
        }

        public async Task<List<SupplierPriceBusinessModel>> GetBySupplierIdAsync(string supplierId, string groupId)
        {
            var models = await _context.SupplierPrices
                .Where(p => p.SupplierId == supplierId && p.GroupId == groupId)
                .Include(p => p.Product)
                .ToListAsync();
            return _mapper.Map<List<SupplierPriceBusinessModel>>(models);
        }
    }
}