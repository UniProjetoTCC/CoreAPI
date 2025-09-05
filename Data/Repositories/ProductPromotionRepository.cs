using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Data.Repositories
{
    public class ProductPromotionRepository : IProductPromotionRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public ProductPromotionRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<ProductBusinessModel>> GetProductsInPromotionAsync(string promotionId, string groupId)
        {
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == promotionId && p.GroupId == groupId);

            if (promotion == null)
            {
                return new List<ProductBusinessModel>();
            }

            var products = await _context.ProductPromotions
                .Where(pp => pp.PromotionId == promotionId)
                .Include(pp => pp.Product)
                .Select(pp => pp.Product)
                .ToListAsync();

            return _mapper.Map<List<ProductBusinessModel>>(products);
        }

        public async Task<ProductPromotionBusinessModel?> AddProductToPromotionAsync(string productId, string promotionId, string groupId)
        {
            // Check if promotion exists and belongs to the group
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == promotionId && p.GroupId == groupId);

            if (promotion == null)
            {
                return null;
            }

            // Check if product exists and belongs to the group
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.GroupId == groupId);

            if (product == null)
            {
                return null;
            }

            // Check if the product is already in the promotion
            var existingRelation = await _context.ProductPromotions
                .FirstOrDefaultAsync(pp => pp.ProductId == productId && pp.PromotionId == promotionId);

            if (existingRelation != null)
            {
                return null; // Product is already in the promotion
            }

            var productPromotion = new ProductPromotionModel
            {
                ProductId = productId,
                PromotionId = promotionId,
                GroupId = groupId
            };

            _context.ProductPromotions.Add(productPromotion);
            await _context.SaveChangesAsync();

            // Carregar as entidades relacionadas para o mapeamento
            await _context.Entry(productPromotion).Reference(pp => pp.Product).LoadAsync();
            await _context.Entry(productPromotion).Reference(pp => pp.Promotion).LoadAsync();

            return _mapper.Map<ProductPromotionBusinessModel>(productPromotion);
        }

        public async Task<bool> RemoveProductFromPromotionAsync(string productId, string promotionId, string groupId)
        {
            // Check if promotion exists and belongs to the group
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == promotionId && p.GroupId == groupId);

            if (promotion == null)
            {
                return false;
            }

            // Find the product-promotion relation
            var productPromotion = await _context.ProductPromotions
                .FirstOrDefaultAsync(pp => pp.ProductId == productId && pp.PromotionId == promotionId);

            if (productPromotion == null)
            {
                return false; // Product is not in the promotion
            }

            _context.ProductPromotions.Remove(productPromotion);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<int> AddProductsToPromotionAsync(List<string> productIds, string promotionId, string groupId)
        {
            // Check if promotion exists and belongs to the group
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == promotionId && p.GroupId == groupId);

            if (promotion == null)
            {
                return 0;
            }

            // Get products that belong to the group
            var validProducts = await _context.Products
                .Where(p => productIds.Contains(p.Id) && p.GroupId == groupId)
                .Select(p => p.Id)
                .ToListAsync();

            // Get existing product-promotion relations
            var existingRelations = await _context.ProductPromotions
                .Where(pp => pp.PromotionId == promotionId && validProducts.Contains(pp.ProductId))
                .Select(pp => pp.ProductId)
                .ToListAsync();

            // Filter out products that are already in the promotion
            var productsToAdd = validProducts.Except(existingRelations).ToList();

            // Create new product-promotion relations
            var productPromotions = productsToAdd.Select(productId => new ProductPromotionModel
            {
                ProductId = productId,
                PromotionId = promotionId,
                GroupId = groupId
            }).ToList();

            _context.ProductPromotions.AddRange(productPromotions);
            await _context.SaveChangesAsync();

            return productPromotions.Count();
        }

        public async Task<int> RemoveProductsFromPromotionAsync(List<string> productIds, string promotionId, string groupId)
        {
            // Check if promotion exists and belongs to the group
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == promotionId && p.GroupId == groupId);

            if (promotion == null)
            {
                return 0;
            }

            // Find the product-promotion relations to remove
            var productPromotions = await _context.ProductPromotions
                .Where(pp => pp.PromotionId == promotionId && productIds.Contains(pp.ProductId))
                .ToListAsync();

            if (productPromotions.Count == 0)
            {
                return 0;
            }

            _context.ProductPromotions.RemoveRange(productPromotions);
            await _context.SaveChangesAsync();

            return productPromotions.Count();
        }

        public async Task<int> RemoveAllProductsFromPromotionAsync(string promotionId, string groupId)
        {
            // Check if promotion exists and belongs to the group
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == promotionId && p.GroupId == groupId);

            if (promotion == null)
            {
                return 0;
            }

            // Find all product-promotion relations for this promotion
            var productPromotions = await _context.ProductPromotions
                .Where(pp => pp.PromotionId == promotionId)
                .ToListAsync();

            if (productPromotions.Count == 0)
            {
                return 0;
            }

            _context.ProductPromotions.RemoveRange(productPromotions);
            await _context.SaveChangesAsync();

            return productPromotions.Count();
        }

        public async Task<List<PromotionBusinessModel>> GetPromotionsByProductIdAsync(string productId, string groupId)
        {
            var promotions = await _context.ProductPromotions
                .Where(pp => pp.Product != null && pp.Promotion != null && pp.ProductId == productId && pp.Product.GroupId == groupId && pp.Promotion.IsActive)
                .Select(pp => pp.Promotion)
                .ToListAsync();

            return _mapper.Map<List<PromotionBusinessModel>>(promotions);          
        }
    }
}
