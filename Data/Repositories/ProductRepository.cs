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

        public async Task<ProductBusinessModel?> CreateProductAsync(ProductBusinessModel product)
        {
            if (product == null)
                return null;

            var newProduct = _mapper.Map<ProductModel>(product);

            await _context.Products.AddAsync(newProduct);
            await _context.SaveChangesAsync();

            return _mapper.Map<ProductBusinessModel>(newProduct);
        }

        public async Task<ProductBusinessModel?> UpdateProductAsync(ProductBusinessModel product)
        {
            if (product == null)
                return null;

            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == product.Id && p.GroupId == product.GroupId);
            
            if (existingProduct == null)
                return null;

            _mapper.Map(product, existingProduct);
            
            existingProduct.UpdatedAt = DateTime.UtcNow;
            
            _context.Products.Update(existingProduct);
            await _context.SaveChangesAsync();

            return _mapper.Map<ProductBusinessModel>(existingProduct);
        }
    }
}
