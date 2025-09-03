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
    public class LoyaltyProgramRepository : ILoyaltyProgramRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public LoyaltyProgramRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<LoyaltyProgramBusinessModel>> GetAllAsync(string groupId)
        {
            var loyaltyPrograms = await _context.LoyaltyPrograms
                .Where(p => p.GroupId == groupId)
                .ToListAsync();

            return _mapper.Map<List<LoyaltyProgramBusinessModel>>(loyaltyPrograms);
        }

        public async Task<LoyaltyProgramBusinessModel?> GetByIdAsync(string id, string groupId)
        {
            var loyaltyProgram = await _context.LoyaltyPrograms
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (loyaltyProgram == null)
                return null;

            return _mapper.Map<LoyaltyProgramBusinessModel>(loyaltyProgram);
        }

        public async Task<List<LoyaltyProgramBusinessModel>> GetActiveAsync(string groupId)
        {
            var loyaltyPrograms = await _context.LoyaltyPrograms
                .Where(p => p.GroupId == groupId && p.IsActive)
                .ToListAsync();

            return _mapper.Map<List<LoyaltyProgramBusinessModel>>(loyaltyPrograms);
        }

        public async Task<LoyaltyProgramBusinessModel?> CreateAsync(
            string name,
            string description,
            decimal centsToPoints,
            decimal pointsToCents,
            decimal discountPercentage,
            string groupId,
            bool isActive = true)
        {
            var loyaltyProgram = new LoyaltyProgramModel
            {
                Name = name,
                Description = description,
                CentsToPoints = centsToPoints,
                PointsToCents = pointsToCents,
                DiscountPercentage = discountPercentage,
                GroupId = groupId,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };

            await _context.LoyaltyPrograms.AddAsync(loyaltyProgram);
            await _context.SaveChangesAsync();

            return _mapper.Map<LoyaltyProgramBusinessModel>(loyaltyProgram);
        }

        public async Task<LoyaltyProgramBusinessModel?> UpdateAsync(
            string id,
            string groupId,
            string name,
            string description,
            decimal centsToPoints,
            decimal pointsToCents,
            decimal discountPercentage)
        {
            var loyaltyProgram = await _context.LoyaltyPrograms
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (loyaltyProgram == null)
                return null;

            loyaltyProgram.Name = name;
            loyaltyProgram.Description = description;
            loyaltyProgram.CentsToPoints = centsToPoints;
            loyaltyProgram.PointsToCents = pointsToCents;
            loyaltyProgram.DiscountPercentage = discountPercentage;
            loyaltyProgram.UpdatedAt = DateTime.UtcNow;

            _context.LoyaltyPrograms.Update(loyaltyProgram);
            await _context.SaveChangesAsync();

            return _mapper.Map<LoyaltyProgramBusinessModel>(loyaltyProgram);
        }

        public async Task<LoyaltyProgramBusinessModel?> ActivateAsync(string id, string groupId)
        {
            var loyaltyProgram = await _context.LoyaltyPrograms
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (loyaltyProgram == null)
                return null;

            loyaltyProgram.IsActive = true;
            loyaltyProgram.UpdatedAt = DateTime.UtcNow;

            _context.LoyaltyPrograms.Update(loyaltyProgram);
            await _context.SaveChangesAsync();

            return _mapper.Map<LoyaltyProgramBusinessModel>(loyaltyProgram);
        }

        public async Task<LoyaltyProgramBusinessModel?> DeactivateAsync(string id, string groupId)
        {
            var loyaltyProgram = await _context.LoyaltyPrograms
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (loyaltyProgram == null)
                return null;

            loyaltyProgram.IsActive = false;
            loyaltyProgram.UpdatedAt = DateTime.UtcNow;

            _context.LoyaltyPrograms.Update(loyaltyProgram);
            await _context.SaveChangesAsync();

            return _mapper.Map<LoyaltyProgramBusinessModel>(loyaltyProgram);
        }

        public async Task<LoyaltyProgramBusinessModel?> DeleteAsync(string id, string groupId)
        {
            var loyaltyProgram = await _context.LoyaltyPrograms
                .FirstOrDefaultAsync(p => p.Id == id && p.GroupId == groupId);

            if (loyaltyProgram == null)
                return null;

            _context.LoyaltyPrograms.Remove(loyaltyProgram);
            await _context.SaveChangesAsync();

            return _mapper.Map<LoyaltyProgramBusinessModel>(loyaltyProgram);
        }
    }
}
