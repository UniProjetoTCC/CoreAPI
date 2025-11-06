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
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public PurchaseOrderRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PurchaseOrderBusinessModel?> GetByIdAsync(string id, string groupId)
        {
            var order = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Items!)
                    .ThenInclude(item => item.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(po => po.Id == id && po.GroupId == groupId);

            return _mapper.Map<PurchaseOrderBusinessModel>(order);
        }
        
        public async Task<PurchaseOrderBusinessModel?> GetByOrderNumberAsync(string orderNumber, string groupId)
        {
             var order = await _context.PurchaseOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(po => po.OrderNumber.ToLower() == orderNumber.ToLower() && po.GroupId == groupId);

            return _mapper.Map<PurchaseOrderBusinessModel>(order);
        }

        public async Task<PurchaseOrderBusinessModel> CreateAsync(PurchaseOrderBusinessModel orderBusinessModel)
        {
            var order = _mapper.Map<PurchaseOrderModel>(orderBusinessModel);
            order.Id = Guid.NewGuid().ToString();
            order.CreatedAt = DateTime.UtcNow;

            foreach (var item in order.Items!)
            {
                item.Id = Guid.NewGuid().ToString();
                item.PurchaseOrderId = order.Id;
                item.CreatedAt = DateTime.UtcNow;
            }

            _context.PurchaseOrders.Add(order);
            await _context.SaveChangesAsync();
            
            // Recarregar para incluir Supplier e Itens.Produto
            var createdOrder = await GetByIdAsync(order.Id, order.GroupId);
            return createdOrder!;
        }
        
        public async Task<PurchaseOrderBusinessModel?> UpdateAsync(PurchaseOrderBusinessModel orderBusinessModel)
        {
            var order = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == orderBusinessModel.Id && po.GroupId == orderBusinessModel.GroupId);

            if (order == null) return null;

            // Mapear apenas os campos permitidos para atualização (ex: Status, DeliveryDate)
            order.Status = orderBusinessModel.Status;
            order.DeliveryDate = orderBusinessModel.DeliveryDate;
            order.TotalAmount = orderBusinessModel.TotalAmount; // Se o total puder ser recalculado
            order.UpdatedAt = DateTime.UtcNow;
            
            // Lógica para atualizar itens (mais complexa, pode exigir remoção/adição)
            // ...

            _context.PurchaseOrders.Update(order);
            await _context.SaveChangesAsync();

            return _mapper.Map<PurchaseOrderBusinessModel>(order);
        }

        public async Task<bool> DeleteAsync(string id, string groupId)
        {
            var order = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == id && po.GroupId == groupId);

            if (order == null) return false;
            
            // A deleção em cascata (configurada no Context) deve remover os Itens.
            _context.PurchaseOrders.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(List<PurchaseOrderBusinessModel> Items, int TotalCount)> SearchAsync(
            string groupId, DateTime startDate, DateTime endDate, string? supplierId, string? status, int page, int pageSize)
        {
            var utcStartDate = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
            var endDateInclusive = endDate.Date.AddDays(1).AddTicks(-1);

            var query = _context.PurchaseOrders
                .AsNoTracking()
                .Where(po => po.GroupId == groupId &&
                             po.OrderDate >= utcStartDate &&
                             po.OrderDate <= endDateInclusive);

            if (!string.IsNullOrEmpty(supplierId))
                query = query.Where(po => po.SupplierId == supplierId);
            
            if (!string.IsNullOrEmpty(status))
                query = query.Where(po => po.Status == status);

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(po => po.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(po => po.Supplier)
                .Include(po => po.Items!)
                    .ThenInclude(item => item.Product)
                .ToListAsync();

            return (_mapper.Map<List<PurchaseOrderBusinessModel>>(orders), totalCount);
        }
    }
}