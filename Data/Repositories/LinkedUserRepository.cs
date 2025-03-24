using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Data.Models;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Data.Repositories
{
    public class LinkedUserRepository : ILinkedUserRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LinkedUserRepository> _logger;

        public LinkedUserRepository(
            CoreAPIContext context,
            IMapper mapper,
            IUserGroupRepository userGroupRepository,
            UserManager<IdentityUser> userManager,
            ILogger<LinkedUserRepository> logger)
        {
            _context = context;
            _mapper = mapper;
            _userGroupRepository = userGroupRepository;
            _userManager = userManager;
            _logger = logger;
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


        public async Task<LinkedUser?> UpdateLinkedUserAsync(
            string linkedUserId,
            bool canPerformTransactions,
            bool canGenerateReports,
            bool canManageProducts,
            bool canAlterStock,
            bool canManagePromotions)
        {
            var linkedUser = await _context.LinkedUsers
                .Where(lu => lu.LinkedUserId == linkedUserId)
                .FirstOrDefaultAsync();
            if (linkedUser == null)
            {
                return null;
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

        public async Task<bool> DeleteLinkedUserAsync(string linkedUserId)
        {
            var linkedUser = await _context.LinkedUsers
                                   .Where(lu => lu.LinkedUserId == linkedUserId)
                                   .FirstOrDefaultAsync();
            if (linkedUser == null)
            {
                return false;
            }

            _context.LinkedUsers.Remove(linkedUser);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<LinkedUser>> GetAllByGroupIdAsync(int groupId)
        {
            var linkedUsers = await _context.LinkedUsers
                .Where(l => l.GroupId == groupId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<LinkedUser>>(linkedUsers);
        }

        public async Task DeactivateLinkedUsersAsync(IEnumerable<int> linkedUserIds)
        {
            var linkedUsers = await _context.LinkedUsers
                .Where(lu => linkedUserIds.Contains(lu.Id))
                .ToListAsync();

            foreach (var linkedUser in linkedUsers)
            {
                linkedUser.IsActive = false;
                linkedUser.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsLinkedUserAsync(string userId)
        {
            return await _context.LinkedUsers.AnyAsync(lu => lu.LinkedUserId == userId);
        }

        public async Task ActivateLinkedUsersAsync(IEnumerable<int> linkedUserIds)
        {
            var linkedUsers = await _context.LinkedUsers
                .Where(lu => linkedUserIds.Contains(lu.Id))
                .ToListAsync();

            foreach (var linkedUser in linkedUsers)
            {
                linkedUser.IsActive = true;
                linkedUser.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAllLinkedUsersByParentIdAsync(string parentUserId)
        {
            var parentUser = await _userManager.FindByIdAsync(parentUserId);
            if (parentUser == null)
            {
                _logger.LogWarning($"Parent user {parentUserId} not found");
                return false;
            }

            var linkedUsers = await _context.LinkedUsers
                .Where(lu => lu.ParentUserId == parentUserId)
                .ToListAsync();

            // If no linked users, return success
            if (linkedUsers == null || !linkedUsers.Any())
            {
                _logger.LogInformation($"No linked users found for parent {parentUserId}");
                return true;
            }

            // Start a database transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var linkedUser in linkedUsers)
                {
                    // Get the linked user identity
                    var linkedUserIdentity = await _userManager.FindByIdAsync(linkedUser.LinkedUserId);
                    if (linkedUserIdentity != null)
                    {
                        // Delete linked user from repository
                        var deleteLinkedResult = await DeleteLinkedUserAsync(linkedUser.LinkedUserId);
                        if (!deleteLinkedResult)
                        {
                            _logger.LogWarning($"Failed to delete linked user {linkedUser.LinkedUserId} from repository");
                            await transaction.RollbackAsync();
                            return false;
                        }
                        
                        // Delete linked user from identity
                        var deleteIdentityResult = await _userManager.DeleteAsync(linkedUserIdentity);
                        if (!deleteIdentityResult.Succeeded)
                        {
                            _logger.LogWarning($"Failed to delete linked user {linkedUser.LinkedUserId} from identity: {string.Join(", ", deleteIdentityResult.Errors.Select(e => e.Description))}");
                            await transaction.RollbackAsync();
                            return false;
                        }
                    }
                }

                // Commit the transaction if all operations succeeded
                await transaction.CommitAsync();
                _logger.LogInformation($"Successfully deleted all linked users for parent {parentUserId}");
                return true;
            }
            catch (Exception ex)
            {
                // Roll back the transaction if an exception occurs
                _logger.LogError(ex, $"Error deleting linked users for parent {parentUserId}");
                await transaction.RollbackAsync();
                return false;
            }
        }

    }
}
