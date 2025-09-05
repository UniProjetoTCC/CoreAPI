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
    public class PaymentMethodRepository : IPaymentMethodRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public PaymentMethodRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<PaymentMethodBusinessModel>> GetAllAsync(string groupId)
        {
            var paymentMethods = await _context.PaymentMethods
                .Where(p => p.GroupId == groupId)
                .ToListAsync();

            return _mapper.Map<List<PaymentMethodBusinessModel>>(paymentMethods);
        }

        public async Task<PaymentMethodBusinessModel?> GetByIdAsync(string id, string groupId)
        {
            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (paymentMethod == null)
                return null;

            return _mapper.Map<PaymentMethodBusinessModel>(paymentMethod);
        }

        public async Task<List<PaymentMethodBusinessModel>> GetActiveAsync(string groupId)
        {
            var paymentMethods = await _context.PaymentMethods
                .Where(p => p.GroupId == groupId && p.Active)
                .ToListAsync();

            return _mapper.Map<List<PaymentMethodBusinessModel>>(paymentMethods);
        }

        public async Task<PaymentMethodBusinessModel?> CreateAsync(
            string name,
            string code,
            string description,
            string groupId,
            bool isActive = true)
        {
            var paymentMethod = new PaymentMethodModel
            {
                Name = name,
                Code = code,
                Description = description,
                GroupId = groupId,
                Active = isActive,
                CreatedAt = DateTime.UtcNow
            };

            await _context.PaymentMethods.AddAsync(paymentMethod);
            await _context.SaveChangesAsync();

            return _mapper.Map<PaymentMethodBusinessModel>(paymentMethod);
        }

        public async Task<PaymentMethodBusinessModel?> UpdateAsync(
            string id,
            string groupId,
            string name,
            string code,
            string description)
        {
            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (paymentMethod == null)
                return null;

            paymentMethod.Name = name;
            paymentMethod.Code = code;
            paymentMethod.Description = description;
            paymentMethod.UpdatedAt = DateTime.UtcNow;

            _context.PaymentMethods.Update(paymentMethod);
            await _context.SaveChangesAsync();

            return _mapper.Map<PaymentMethodBusinessModel>(paymentMethod);
        }

        public async Task<PaymentMethodBusinessModel?> ActivateAsync(string id, string groupId)
        {
            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (paymentMethod == null)
                return null;

            paymentMethod.Active = true;
            paymentMethod.UpdatedAt = DateTime.UtcNow;

            _context.PaymentMethods.Update(paymentMethod);
            await _context.SaveChangesAsync();

            return _mapper.Map<PaymentMethodBusinessModel>(paymentMethod);
        }

        public async Task<PaymentMethodBusinessModel?> DeactivateAsync(string id, string groupId)
        {
            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (paymentMethod == null)
                return null;

            paymentMethod.Active = false;
            paymentMethod.UpdatedAt = DateTime.UtcNow;

            _context.PaymentMethods.Update(paymentMethod);
            await _context.SaveChangesAsync();

            return _mapper.Map<PaymentMethodBusinessModel>(paymentMethod);
        }

        public async Task<PaymentMethodBusinessModel?> DeleteAsync(string id, string groupId)
        {
            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (paymentMethod == null)
                return null;

            _context.PaymentMethods.Remove(paymentMethod);
            await _context.SaveChangesAsync();

            return _mapper.Map<PaymentMethodBusinessModel>(paymentMethod);
        }
    }
}
