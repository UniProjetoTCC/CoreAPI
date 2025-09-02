using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Business.Utils;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public ProductRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ProductBusinessModel?> GetById(string id, string groupId)
        {
            var product = await _context.Products
                .Include(p => p.UserGroup)
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            return product != null ? _mapper.Map<ProductBusinessModel>(product) : null;
        }

        public async Task<(List<ProductBusinessModel> Items, int TotalCount)> SearchByNameAsync(
            string name,
            string groupId,
            int page = 1,
            int pageSize = 20)
        {
            // Only bail out if missing groupId; empty name will match all products
            if (string.IsNullOrEmpty(groupId))
                return (new List<ProductBusinessModel>(), 0);

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            // Normalize input; empty name becomes empty string
            string normalizedName = StringUtils.RemoveDiacritics(name ?? "");

            // Build query: filter by group; optionally filter by name if provided
            var query = _context.Products.Where(p => p.GroupId == groupId);
            if (!string.IsNullOrWhiteSpace(normalizedName))
            {
                query = query.Where(p =>
                    EF.Functions.ILike(p.Name, $"%{normalizedName}%") ||
                    (p.Description != null && EF.Functions.ILike(p.Description, $"%{normalizedName}%"))
                );
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination in memory-efficient way
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (_mapper.Map<List<ProductBusinessModel>>(products), totalCount);
        }

        public async Task<ProductBusinessModel?> CreateProductAsync(
            string groupId,
            string categoryId,
            string name,
            string sku,
            string barCode,
            string? description,
            decimal price,
            decimal cost,
            bool active = true)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(categoryId))
            {
                return null;
            }

            // Normalize product name and description to remove accents
            string normalizedName = StringUtils.RemoveDiacritics(name);
            string? normalizedDescription = description != null ? StringUtils.RemoveDiacritics(description) : null;

            var newProduct = new ProductModel
            {
                GroupId = groupId,
                CategoryId = categoryId,
                Name = normalizedName,
                SKU = sku,
                BarCode = barCode,
                Description = normalizedDescription,
                Price = price,
                Cost = cost,
                Active = active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Products.AddAsync(newProduct);
            await _context.SaveChangesAsync();

            return _mapper.Map<ProductBusinessModel>(newProduct);
        }

        public async Task<ProductBusinessModel?> UpdateProductAsync(
            string id,
            string groupId,
            string categoryId,
            string name,
            string sku,
            string barCode,
            string? description,
            decimal cost,
            bool active)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(categoryId) || string.IsNullOrEmpty(groupId))
            {
                return null;
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (product == null)
            {
                return null;
            }

            // Normalize product name and description to remove accents
            string normalizedName = StringUtils.RemoveDiacritics(name);
            string? normalizedDescription = description != null ? StringUtils.RemoveDiacritics(description) : null;

            product.CategoryId = categoryId;
            product.Name = normalizedName; // Store normalized name
            product.SKU = sku;
            product.BarCode = barCode;
            product.Description = normalizedDescription; // Store normalized description
            product.Cost = cost;
            product.Active = active;
            product.UpdatedAt = DateTime.UtcNow;

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return _mapper.Map<ProductBusinessModel>(product);
        }

        public async Task<ProductBusinessModel?> DeleteProductAsync(string id, string groupId)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(groupId))
            {
                return null;
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (product == null)
            {
                return null;
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return _mapper.Map<ProductBusinessModel>(product);
        }

        public async Task<bool> HasProductsInCategoryAsync(string categoryId, string groupId)
        {
            if (string.IsNullOrEmpty(categoryId) || string.IsNullOrEmpty(groupId))
            {
                return false;
            }

            return await _context.Products
                .AnyAsync(p => p.CategoryId == categoryId && p.GroupId == groupId);
        }

        public async Task<List<ProductBusinessModel>> GetProductsByBarCodeAsync(string barCode, string groupId)
        {
            if (string.IsNullOrEmpty(barCode) || string.IsNullOrEmpty(groupId))
            {
                return new List<ProductBusinessModel>();
            }

            var products = await _context.Products
                .Where(p => p.BarCode == barCode && p.GroupId == groupId)
                .ToListAsync();

            return _mapper.Map<List<ProductBusinessModel>>(products);
        }

        public async Task<List<ProductBusinessModel>> GetProductsBySKUAsync(string sku, string groupId)
        {
            if (string.IsNullOrEmpty(sku) || string.IsNullOrEmpty(groupId))
            {
                return new List<ProductBusinessModel>();
            }

            var products = await _context.Products
                .Where(p => p.SKU == sku && p.GroupId == groupId)
                .ToListAsync();

            return _mapper.Map<List<ProductBusinessModel>>(products);
        }

        public async Task<(bool IsBarcodeDuplicate, bool IsSKUDuplicate, ProductBusinessModel? Product)> CheckDuplicateProductAsync(string barCode, string sku, string groupId)
        {
            if (string.IsNullOrEmpty(groupId) || (string.IsNullOrEmpty(barCode) && string.IsNullOrEmpty(sku)))
            {
                return (false, false, null);
            }

            // Single query using OR condition for both barcode and SKU
            var products = await _context.Products
                .Where(p => p.GroupId == groupId &&
                          ((!string.IsNullOrEmpty(barCode) && p.BarCode == barCode) ||
                           (!string.IsNullOrEmpty(sku) && p.SKU == sku)))
                .ToListAsync();

            if (!products.Any())
            {
                return (false, false, null);
            }

            // Check which field caused the duplicate
            bool isBarcodeDuplicate = !string.IsNullOrEmpty(barCode) && products.Any(p => p.BarCode == barCode);
            bool isSKUDuplicate = !string.IsNullOrEmpty(sku) && products.Any(p => p.SKU == sku);

            // Return the first matching product
            return (isBarcodeDuplicate, isSKUDuplicate, _mapper.Map<ProductBusinessModel>(products.First()));
        }

        public async Task<ProductBusinessModel?> UpdateProductPriceAsync(
            string id,
            string groupId,
            decimal newPrice,
            string userId,
            string? reason = null)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(userId))
            {
                return null;
            }

            // Use a transaction to ensure both the product update and price history creation are atomic
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get the product
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

                if (product == null)
                {
                    return null;
                }

                // Store the old price for history
                decimal oldPrice = product.Price;

                // Create a price history record
                var priceHistory = new PriceHistoryModel
                {
                    ProductId = id,
                    GroupId = groupId,
                    ChangedByUserId = userId,
                    OldPrice = oldPrice,
                    NewPrice = newPrice,
                    ChangeDate = DateTime.UtcNow,
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow
                };

                // Update the product price
                product.Price = newPrice;
                product.UpdatedAt = DateTime.UtcNow;

                // Save both changes
                await _context.PriceHistories.AddAsync(priceHistory);
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                return _mapper.Map<ProductBusinessModel>(product);
            }
            catch (Exception)
            {
                // Rollback the transaction if any error occurs
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
