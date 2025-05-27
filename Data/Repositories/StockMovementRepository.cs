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
    public class StockMovementRepository : IStockMovementRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public StockMovementRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<StockMovementBusinessModel?> GetByIdAsync(string id)
        {
            var movementModel = await _context.StockMovements
                .Include(sm => sm.Stock)
                .ThenInclude(s => s != null ? s.Product : null)
                .FirstOrDefaultAsync(sm => sm.Id == id);
                
            return _mapper.Map<StockMovementBusinessModel>(movementModel);
        }

        public async Task<List<StockMovementBusinessModel>> GetByStockIdAsync(string stockId, int page = 1, int pageSize = 20)
        {
            var movementModels = await _context.StockMovements
                .Where(sm => sm.StockId == stockId)
                .OrderByDescending(sm => sm.MovementDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(sm => sm.Stock)
                .ThenInclude(s => s != null ? s.Product : null)
                .ToListAsync();
                
            return _mapper.Map<List<StockMovementBusinessModel>>(movementModels);
        }

        public async Task<List<StockMovementBusinessModel>> GetByProductIdAsync(string productId, string groupId, int page = 1, int pageSize = 20)
        {
            var movementModels = await _context.StockMovements
                .Include(sm => sm.Stock)
                .Where(sm => sm.Stock != null && sm.Stock.ProductId == productId && sm.Stock.GroupId == groupId)
                .OrderByDescending(sm => sm.MovementDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
                
            return _mapper.Map<List<StockMovementBusinessModel>>(movementModels);
        }

        public async Task<StockMovementBusinessModel> AddMovementAsync(StockMovementBusinessModel movement)
        {
            var movementModel = _mapper.Map<StockMovementModel>(movement);
            await _context.StockMovements.AddAsync(movementModel);
            await _context.SaveChangesAsync();
            return _mapper.Map<StockMovementBusinessModel>(movementModel);
        }

        public async Task<int> GetTotalMovementsForProductAsync(string productId, string groupId)
        {
            return await _context.StockMovements
                .Include(sm => sm.Stock)
                .Where(sm => sm.Stock != null && sm.Stock.ProductId == productId && sm.Stock.GroupId == groupId)
                .CountAsync();
        }
    }
}
