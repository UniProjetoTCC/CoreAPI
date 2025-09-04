using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public SupplierRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<SupplierBusinessModel> CreateAsync(SupplierBusinessModel supplier)
        {
            var model = _mapper.Map<SupplierModel>(supplier);
            await _context.Suppliers.AddAsync(model);
            await _context.SaveChangesAsync();
            return _mapper.Map<SupplierBusinessModel>(model);
        }

        public async Task<SupplierBusinessModel?> UpdateAsync(SupplierBusinessModel supplier)
        {
            var model = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplier.Id && s.GroupId == supplier.GroupId);
            if (model == null) return null;

            _mapper.Map(supplier, model);
            model.UpdatedAt = DateTime.UtcNow;

            _context.Suppliers.Update(model);
            await _context.SaveChangesAsync();

            return _mapper.Map<SupplierBusinessModel>(model);
        }

        public async Task<bool> DeleteAsync(string id, string groupId)
        {
            var model = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id && s.GroupId == groupId);
            if (model == null) return false;

            _context.Suppliers.Remove(model);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SupplierBusinessModel?> GetByDocumentAsync(string document, string groupId)
        {
            var model = await _context.Suppliers.FirstOrDefaultAsync(s => s.Document == document && s.GroupId == groupId);
            return _mapper.Map<SupplierBusinessModel>(model);
        }

        public async Task<SupplierBusinessModel?> GetByIdAsync(string id, string groupId)
        {
            var model = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id && s.GroupId == groupId);
            return _mapper.Map<SupplierBusinessModel>(model);
        }

        public async Task<(List<SupplierBusinessModel> Items, int TotalCount)> SearchAsync(string groupId, string? name, int page, int pageSize)
        {
            var query = _context.Suppliers.Where(s => s.GroupId == groupId);

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(s => EF.Functions.ILike(s.Name, $"%{name}%") || EF.Functions.ILike(s.ContactPerson, $"%{name}%"));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (_mapper.Map<List<SupplierBusinessModel>>(items), totalCount);
        }

        
    }
}