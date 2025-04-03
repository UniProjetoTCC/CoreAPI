using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business.Models;

namespace Business.DataRepositories
{
    public interface IProductRepository
    {
        Task<ProductBusinessModel?> GetById(string id, string groupId);
        
        Task<ProductBusinessModel?> CreateProductAsync(
            string groupId,
            string categoryId,
            string name,
            string sku,
            string barCode,
            string? description,
            decimal price,
            decimal cost,
            bool active = true
        );
        
        Task<ProductBusinessModel?> UpdateProductAsync(
            string id,
            string groupId,
            string categoryId,
            string name,
            string sku,
            string barCode,
            string? description,
            decimal price,
            decimal cost,
            bool active
        );
    }
}