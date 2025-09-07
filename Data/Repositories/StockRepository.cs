using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class StockRepository : IStockRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public StockRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<StockBusinessModel?> GetByProductIdAsync(string productId, string groupId)
        {
            var stockModel = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.GroupId == groupId);

            return _mapper.Map<StockBusinessModel>(stockModel);
        }

        public async Task<List<StockBusinessModel>> GetByIdsAsync(IEnumerable<string> ids, string groupId)
        {
            var stocks = await _context.Stocks
                .Where(s => ids.Contains(s.Id) && s.GroupId == groupId)
                .ToListAsync();

            return _mapper.Map<List<StockBusinessModel>>(stocks);
        }

        public async Task<StockBusinessModel?> AddStockAsync(string productId, string groupId, int quantity, string userId, string? reason = null)
        {
            if (quantity <= 0)
            {
                return null;
            }

            var existingStockModel = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.GroupId == groupId);

            if (existingStockModel != null)
            {
                // Update existing stock
                existingStockModel.Quantity += quantity;
                existingStockModel.UpdatedAt = DateTime.UtcNow;
                _context.Stocks.Update(existingStockModel);
            }
            else
            {
                // Create new stock entry
                existingStockModel = new StockModel
                {
                    ProductId = productId,
                    GroupId = groupId,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Stocks.AddAsync(existingStockModel);
            }

            // Create stock movement record
            var movement = new StockMovementModel
            {
                StockId = existingStockModel.Id,
                UserId = userId,
                GroupId = groupId,
                Quantity = (int)quantity,
                MovementType = "Addition",
                MovementDate = DateTime.UtcNow,
                Reason = reason ?? "Stock adjustment",
                CreatedAt = DateTime.UtcNow
            };
            await _context.StockMovements.AddAsync(movement);

            await _context.SaveChangesAsync();
            return _mapper.Map<StockBusinessModel>(existingStockModel);
        }

        public async Task<StockBusinessModel?> UpdateStockAsync(string productId, string groupId, int quantity, string userId, string? reason = null)
        {
            var existingStockModel = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.GroupId == groupId);

            if (existingStockModel == null)
            {
                return null;
            }

            int difference = quantity - existingStockModel.Quantity;
            existingStockModel.Quantity = quantity;
            existingStockModel.UpdatedAt = DateTime.UtcNow;
            _context.Stocks.Update(existingStockModel);

            // Create stock movement record only if there's a change
            if (difference != 0)
            {
                var movement = new StockMovementModel
                {
                    StockId = existingStockModel.Id,
                    UserId = userId,
                    GroupId = groupId,
                    Quantity = Math.Abs((int)difference),
                    MovementType = difference > 0 ? "Addition" : "Reduction",
                    MovementDate = DateTime.UtcNow,
                    Reason = reason ?? "Stock update",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.StockMovements.AddAsync(movement);
            }

            await _context.SaveChangesAsync();
            return _mapper.Map<StockBusinessModel>(existingStockModel);
        }

        public async Task<bool> HasStockAsync(string productId, string groupId, int requiredQuantity)
        {
            var stockModel = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.GroupId == groupId);
            return stockModel != null && stockModel.Quantity >= requiredQuantity;
        }

        public async Task<StockBusinessModel?> DeductStockAsync(string productId, string groupId, int quantity, string userId, string? reason = null)
        {
            if (quantity <= 0)
            {
                return null;
            }

            var existingStockModel = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.GroupId == groupId);

            if (existingStockModel == null || existingStockModel.Quantity < quantity)
            {
                return null; // Not enough stock
            }

            existingStockModel.Quantity -= quantity;
            existingStockModel.UpdatedAt = DateTime.UtcNow;
            _context.Stocks.Update(existingStockModel);

            // Create stock movement record
            var movement = new StockMovementModel
            {
                StockId = existingStockModel.Id,
                UserId = userId,
                GroupId = groupId,
                Quantity = (int)quantity,
                MovementType = "Reduction",
                MovementDate = DateTime.UtcNow,
                Reason = reason ?? "Stock deduction",
                CreatedAt = DateTime.UtcNow
            };
            await _context.StockMovements.AddAsync(movement);

            await _context.SaveChangesAsync();
            return _mapper.Map<StockBusinessModel>(existingStockModel);
        }

        public async Task<int> GetTotalStockAsync(string productId, string groupId)
        {
            var stockModel = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.GroupId == groupId);
            return stockModel?.Quantity ?? 0;
        }

        public async Task<List<StockBusinessModel>> GetLowStockProductsAsync(string groupId, int threshold)
        {
            var stockModels = await _context.Stocks
                .Where(s => s.GroupId == groupId && s.Quantity <= threshold)
                .Include(s => s.Product)
                .ToListAsync();

            return _mapper.Map<List<StockBusinessModel>>(stockModels);
        }
    }
}
