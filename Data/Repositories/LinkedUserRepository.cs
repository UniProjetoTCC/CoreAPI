using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Data.Models;
using Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class LinkedUserRepository : ILinkedUserRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;
        private readonly IUserGroupRepository _userGroupRepository;

        public LinkedUserRepository(
            CoreAPIContext context,
            IMapper mapper,
            IUserGroupRepository userGroupRepository)
        {
            _context = context;
            _mapper = mapper;
            _userGroupRepository = userGroupRepository;
        }

        public async Task<LinkedUser?> GetByUserIdAsync(string userId)
        {
            var linkedUser = await _context.LinkedUsers
                .Include(lu => lu.UserGroup)
                .FirstOrDefaultAsync(lu => lu.LinkedUserId == userId);

            return linkedUser != null ? _mapper.Map<LinkedUser>(linkedUser) : null;
        }

        public async Task<LinkedUser?> GetByIdAsync(int id)
        {
            var linkedUser = await _context.LinkedUsers
                .Include(lu => lu.UserGroup)
                .FirstOrDefaultAsync(lu => lu.Id == id);

            return linkedUser != null ? _mapper.Map<LinkedUser>(linkedUser) : null;
        }

        public async Task<IEnumerable<LinkedUser>> GetByCreatedByUserIdAsync(string createdByUserId)
        {
            var linkedUsers = await _context.LinkedUsers
                .Include(lu => lu.UserGroup)
                .Where(lu => lu.ParentUserId == createdByUserId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<LinkedUser>>(linkedUsers);
        }

        public async Task<LinkedUser> CreateLinkedUserAsync(
            string userId,
            string createdByUserId,
            bool canPerformTransactions,
            bool canGenerateReports,
            bool canManageProducts,
            bool canAlterStock,
            bool canManagePromotions)
        {
            // Get the creator's group
            var creatorGroup = await _userGroupRepository.GetByUserIdAsync(createdByUserId);

            var linkedUser = new LinkedUserModel
            {
                LinkedUserId = userId,
                ParentUserId = createdByUserId,
                GroupId = creatorGroup?.GroupId ?? 0,
                CanPerformTransactions = canPerformTransactions,
                CanGenerateReports = canGenerateReports,
                CanManageProducts = canManageProducts,
                CanAlterStock = canAlterStock,
                CanManagePromotions = canManagePromotions,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.LinkedUsers.Add(linkedUser);
            await _context.SaveChangesAsync();

            return _mapper.Map<LinkedUser>(linkedUser);
        }

        public async Task<LinkedUser> UpdateLinkedUserAsync(
            int id,
            bool canPerformTransactions,
            bool canGenerateReports,
            bool canManageProducts,
            bool canAlterStock,
            bool canManagePromotions)
        {
            var linkedUser = await _context.LinkedUsers.FindAsync(id);
            if (linkedUser == null)
            {
                throw new KeyNotFoundException($"LinkedUser with ID {id} not found");
            }

            linkedUser.CanPerformTransactions = canPerformTransactions;
            linkedUser.CanGenerateReports = canGenerateReports;
            linkedUser.CanManageProducts = canManageProducts;
            linkedUser.CanAlterStock = canAlterStock;
            linkedUser.CanManagePromotions = canManagePromotions;
            linkedUser.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<LinkedUser>(linkedUser);
        }

        public async Task<bool> DeleteLinkedUserAsync(int id)
        {
            var linkedUser = await _context.LinkedUsers.FindAsync(id);
            if (linkedUser == null)
            {
                return false;
            }

            _context.LinkedUsers.Remove(linkedUser);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
