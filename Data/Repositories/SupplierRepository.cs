using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<SupplierBusinessModel?> GetByIdAsync(string id, string groupId)
        {
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.GroupId == groupId);

            return supplier == null ? null : _mapper.Map<SupplierBusinessModel>(supplier);
        }

        public async Task<SupplierBusinessModel?> GetByDocumentAsync(string document, string groupId)
        {
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Document == document && s.GroupId == groupId);

            return supplier == null ? null : _mapper.Map<SupplierBusinessModel>(supplier);
        }

        public async Task<SupplierBusinessModel?> GetByEmailAsync(string email, string groupId)
        {
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower() && s.GroupId == groupId);

            return supplier == null ? null : _mapper.Map<SupplierBusinessModel>(supplier);
        }

        public async Task<(IEnumerable<SupplierBusinessModel> Items, int TotalCount)> SearchAsync(
            string groupId,
            string? term,
            int page,
            int pageSize)
        {
            var query = _context.Suppliers
                .AsNoTracking()
                .Where(s => s.GroupId == groupId);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var searchTerm = $"%{term}%";
                query = query.Where(s => 
                    EF.Functions.ILike(s.Name, searchTerm) || 
                    EF.Functions.ILike(s.Document, searchTerm));
            }

            var totalCount = await query.CountAsync();
            var suppliers = await query
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (_mapper.Map<IEnumerable<SupplierBusinessModel>>(suppliers), totalCount);
        }

        public async Task<SupplierBusinessModel> CreateAsync(SupplierBusinessModel supplierBusinessModel)
        {
            var supplier = _mapper.Map<SupplierModel>(supplierBusinessModel);
            supplier.Id = Guid.NewGuid().ToString();
            supplier.CreatedAt = DateTime.UtcNow;
            supplier.IsActive = true; 

            await _context.Suppliers.AddAsync(supplier);
            await _context.SaveChangesAsync();

            return _mapper.Map<SupplierBusinessModel>(supplier);
        }

        public async Task<SupplierBusinessModel?> UpdateAsync(string id, string groupId, SupplierBusinessModel supplierBusinessModel)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == id && s.GroupId == groupId);

            if (supplier == null)
            {
                return null;
            }

            // Atualizar propriedades
            supplier.Name = supplierBusinessModel.Name;
            supplier.Document = supplierBusinessModel.Document;
            supplier.Email = supplierBusinessModel.Email;
            supplier.Phone = supplierBusinessModel.Phone;
            supplier.Address = supplierBusinessModel.Address;
            supplier.ContactPerson = supplierBusinessModel.ContactPerson;
            supplier.PaymentTerms = supplierBusinessModel.PaymentTerms;
            supplier.UpdatedAt = DateTime.UtcNow;

            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();

            return _mapper.Map<SupplierBusinessModel>(supplier);
        }

        public async Task<SupplierBusinessModel?> ActivateAsync(string id, string groupId)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == id && s.GroupId == groupId);

            if (supplier == null)
            {
                return null;
            }

            supplier.IsActive = true;
            supplier.UpdatedAt = DateTime.UtcNow;

            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();

            return _mapper.Map<SupplierBusinessModel>(supplier);
        }

        public async Task<SupplierBusinessModel?> DeactivateAsync(string id, string groupId)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == id && s.GroupId == groupId);

            if (supplier == null)
            {
                return null;
            }

            // TODO: Adicionar verificação se o fornecedor está vinculado a Pedidos de Compra (Purchase Orders) antes de desativar

            supplier.IsActive = false;
            supplier.UpdatedAt = DateTime.UtcNow;

            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();

            return _mapper.Map<SupplierBusinessModel>(supplier);
        }
    }
}