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
    public class SupplierPriceRepository : ISupplierPriceRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public SupplierPriceRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<SupplierPriceBusinessModel?> GetByIdAsync(string id, string groupId)
        {
            var price = await _context.SupplierPrices
                .AsNoTracking()
                .Include(sp => sp.Product)
                .Include(sp => sp.Supplier)
                .FirstOrDefaultAsync(sp => sp.Id == id && sp.GroupId == groupId);

            return price == null ? null : _mapper.Map<SupplierPriceBusinessModel>(price);
        }

        public async Task<(IEnumerable<SupplierPriceBusinessModel> Items, int TotalCount)> GetPricesForProductAsync(
            string productId, 
            string groupId, 
            int page, 
            int pageSize)
        {
            var query = _context.SupplierPrices
                .AsNoTracking()
                .Where(sp => sp.ProductId == productId && sp.GroupId == groupId)
                .Include(sp => sp.Supplier); // Já sabemos o produto, então só incluímos o fornecedor

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(sp => sp.Supplier != null ? sp.Supplier.Name : string.Empty)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (_mapper.Map<IEnumerable<SupplierPriceBusinessModel>>(items), totalCount);
        }

        public async Task<(IEnumerable<SupplierPriceBusinessModel> Items, int TotalCount)> GetPricesFromSupplierAsync(
            string supplierId, 
            string groupId, 
            int page, 
            int pageSize)
        {
            var query = _context.SupplierPrices
                .AsNoTracking()
                .Where(sp => sp.SupplierId == supplierId && sp.GroupId == groupId)
                .Include(sp => sp.Product); // Já sabemos o fornecedor, então só incluímos o produto

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(sp => sp.Product != null ? sp.Product.Name : string.Empty)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (_mapper.Map<IEnumerable<SupplierPriceBusinessModel>>(items), totalCount);
        }

        public async Task<bool> CheckDuplicateAsync(string productId, string supplierId, string supplierSku, string groupId, string? excludeId = null)
        {
            var query = _context.SupplierPrices
                .AsNoTracking()
                .Where(sp => sp.ProductId == productId &&
                             sp.SupplierId == supplierId &&
                             sp.SupplierSku.ToLower() == supplierSku.ToLower() &&
                             sp.GroupId == groupId);

            if (!string.IsNullOrEmpty(excludeId))
            {
                query = query.Where(sp => sp.Id != excludeId);
            }

            return await query.AnyAsync();
        }

        public async Task<SupplierPriceBusinessModel> CreateAsync(SupplierPriceBusinessModel priceBusinessModel)
        {
            var priceModel = _mapper.Map<SupplierPriceModel>(priceBusinessModel);
            priceModel.Id = Guid.NewGuid().ToString();
            priceModel.CreatedAt = DateTime.UtcNow;
            priceModel.IsActive = true;
            priceModel.ValidFrom = priceBusinessModel.ValidFrom == DateTime.MinValue ? DateTime.UtcNow : priceBusinessModel.ValidFrom;

            await _context.SupplierPrices.AddAsync(priceModel);
            await _context.SaveChangesAsync();

            // Recarregar com os dados do Fornecedor e Produto para retornar o DTO completo
            var createdEntry = await _context.SupplierPrices
                .Include(sp => sp.Product)
                .Include(sp => sp.Supplier)
                .FirstOrDefaultAsync(sp => sp.Id == priceModel.Id);

            return _mapper.Map<SupplierPriceBusinessModel>(createdEntry);
        }

        public async Task<SupplierPriceBusinessModel?> UpdateAsync(string id, string groupId, SupplierPriceBusinessModel priceBusinessModel)
        {
            var priceModel = await _context.SupplierPrices
                .FirstOrDefaultAsync(sp => sp.Id == id && sp.GroupId == groupId);

            if (priceModel == null)
            {
                return null;
            }

            // Atualiza os campos
            priceModel.UnitPrice = priceBusinessModel.UnitPrice;
            priceModel.MinimumQuantity = priceBusinessModel.MinimumQuantity;
            priceModel.SupplierSku = priceBusinessModel.SupplierSku;
            priceModel.ValidFrom = priceBusinessModel.ValidFrom == DateTime.MinValue ? priceModel.ValidFrom : priceBusinessModel.ValidFrom;
            priceModel.ValidUntil = priceBusinessModel.ValidUntil;
            priceModel.UpdatedAt = DateTime.UtcNow;

            _context.SupplierPrices.Update(priceModel);
            await _context.SaveChangesAsync();
            
            // Recarregar para incluir Product e Supplier
             await _context.Entry(priceModel).Reference(p => p.Product).LoadAsync();
             await _context.Entry(priceModel).Reference(p => p.Supplier).LoadAsync();

            return _mapper.Map<SupplierPriceBusinessModel>(priceModel);
        }

        public async Task<SupplierPriceBusinessModel?> ActivateAsync(string id, string groupId)
        {
            var priceModel = await _context.SupplierPrices
                .Include(sp => sp.Product)
                .Include(sp => sp.Supplier)
                .FirstOrDefaultAsync(sp => sp.Id == id && sp.GroupId == groupId);

            if (priceModel == null)
            {
                return null;
            }

            priceModel.IsActive = true;
            priceModel.UpdatedAt = DateTime.UtcNow;

            _context.SupplierPrices.Update(priceModel);
            await _context.SaveChangesAsync();

            return _mapper.Map<SupplierPriceBusinessModel>(priceModel);
        }

        public async Task<SupplierPriceBusinessModel?> DeactivateAsync(string id, string groupId)
        {
            var priceModel = await _context.SupplierPrices
                .Include(sp => sp.Product)
                .Include(sp => sp.Supplier)
                .FirstOrDefaultAsync(sp => sp.Id == id && sp.GroupId == groupId);

            if (priceModel == null)
            {
                return null;
            }

            priceModel.IsActive = false;
            priceModel.UpdatedAt = DateTime.UtcNow;

            _context.SupplierPrices.Update(priceModel);
            await _context.SaveChangesAsync();

            return _mapper.Map<SupplierPriceBusinessModel>(priceModel);
        }

        public async Task<bool> DeleteAsync(string id, string groupId)
        {
            var priceModel = await _context.SupplierPrices
                .FirstOrDefaultAsync(sp => sp.Id == id && sp.GroupId == groupId);

            if (priceModel == null)
            {
                return false;
            }

            // TODO: Adicionar verificação se este preço está em algum Pedido de Compra (PurchaseOrder) antes de excluir

            _context.SupplierPrices.Remove(priceModel);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}