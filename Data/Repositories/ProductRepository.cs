using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

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
            if (string.IsNullOrEmpty(groupId))
            {
                throw new ArgumentException("GroupId cannot be null or empty when creating a product");
            }
            
            if (string.IsNullOrEmpty(categoryId))
            {
                throw new ArgumentException("CategoryId cannot be null or empty when creating a product");
            }
            
            // Check if a product with the same barcode already exists in this group
            if (!string.IsNullOrEmpty(barCode))
            {
                var duplicateProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.GroupId == groupId && p.BarCode == barCode);
                    
                if (duplicateProduct != null)
                {
                    throw new InvalidOperationException($"A product with barcode '{barCode}' already exists in this group");
                }
            }
            
            // Check if a product with the same SKU already exists in this group
            if (!string.IsNullOrEmpty(sku))
            {
                var duplicateProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.GroupId == groupId && p.SKU == sku);
                    
                if (duplicateProduct != null)
                {
                    throw new InvalidOperationException($"A product with SKU '{sku}' already exists in this group");
                }
            }

            var newProduct = new ProductModel
            {
                GroupId = groupId,
                CategoryId = categoryId,
                Name = name,
                SKU = sku,
                BarCode = barCode,
                Description = description,
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
            decimal price,
            decimal cost,
            bool active)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Id cannot be null or empty when updating a product");
            }
            
            if (string.IsNullOrEmpty(groupId))
            {
                throw new ArgumentException("GroupId cannot be null or empty when updating a product");
            }
            
            if (string.IsNullOrEmpty(categoryId))
            {
                throw new ArgumentException("CategoryId cannot be null or empty when updating a product");
            }
            
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);
                
            if (existingProduct == null)
            {
                throw new ArgumentException($"Product with id {id} not found in group {groupId}");
            }
            
            // Check if another product already uses this barcode (only if barcode is not empty and not the same as current)
            if (!string.IsNullOrEmpty(barCode) && existingProduct.BarCode != barCode)
            {
                var duplicateProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.GroupId == groupId && p.BarCode == barCode && p.Id != id);
                    
                if (duplicateProduct != null)
                {
                    throw new InvalidOperationException($"Another product with barcode '{barCode}' already exists in this group");
                }
            }
            
            // Check if another product already uses this SKU (only if SKU is not empty and not the same as current)
            if (!string.IsNullOrEmpty(sku) && existingProduct.SKU != sku)
            {
                var duplicateProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.GroupId == groupId && p.SKU == sku && p.Id != id);
                    
                if (duplicateProduct != null)
                {
                    throw new InvalidOperationException($"Another product with SKU '{sku}' already exists in this group");
                }
            }

            existingProduct.CategoryId = categoryId;
            existingProduct.Name = name;
            existingProduct.SKU = sku;
            existingProduct.BarCode = barCode;
            existingProduct.Description = description;
            existingProduct.Price = price;
            existingProduct.Cost = cost;
            existingProduct.Active = active;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            _context.Products.Update(existingProduct);
            await _context.SaveChangesAsync();

            return _mapper.Map<ProductBusinessModel>(existingProduct);
        }
    }
}
