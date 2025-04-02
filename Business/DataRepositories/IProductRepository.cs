using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business.Models;

namespace Business.DataRepositories
{
    public interface IProductRepository
    {
        Task<ProductBusinessModel?> GetById(int id, int groupId);
        Task<ProductBusinessModel?> CreateProductAsync(ProductBusinessModel product);
        Task<ProductBusinessModel?> UpdateProductAsync(ProductBusinessModel product);
    }
}