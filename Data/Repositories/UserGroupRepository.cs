using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using Business.DataRepositories;
using Business.Models;
using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class UserGroupRepository : IUserGroupRepository
    {
        private readonly CoreAPIContext _context;
        private readonly IMapper _mapper;

        public UserGroupRepository(CoreAPIContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<UserGroup?> GetByUserIdAsync(string userId)
        {
            var group = await _context.UserGroups
                .Include(g => g.SubscriptionPlan)
                .FirstOrDefaultAsync(g => g.UserId == userId);

            if (group == null) return null;

            return _mapper.Map<UserGroup>(group);
        }

        public async Task<UserGroup?> GetByGroupIdAsync(string groupId)
        {
            var group = await _context.UserGroups
                .Include(g => g.SubscriptionPlan)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null) return null;

            return _mapper.Map<UserGroup>(group);
        }

        public async Task<UserGroup> CreateGroupAsync(string userId, string subscriptionPlanId)
        {
            var group = new UserGroupModel
            {
                UserId = userId,
                SubscriptionPlanId = subscriptionPlanId,
                SubscriptionStartDate = DateTime.UtcNow,
                SubscriptionEndDate = DateTime.UtcNow.AddMonths(1),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.UserGroups.Add(group);
            await _context.SaveChangesAsync();

            return _mapper.Map<UserGroup>(group);
        }

        public async Task<UserGroup?> UpdateGroupAsync(string groupId, string subscriptionPlanId)
        {
            var group = await _context.UserGroups.FindAsync(groupId);
            if (group == null) return null;

            group.SubscriptionPlanId = subscriptionPlanId;
            group.SubscriptionStartDate = DateTime.UtcNow;
            group.SubscriptionEndDate = DateTime.UtcNow.AddMonths(1);
            group.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return _mapper.Map<UserGroup>(group);
        }

        public async Task DeactivateExpiredUserGroups()
        {
            var groups = await _context.UserGroups
                .Where(g => g.SubscriptionEndDate < DateTime.UtcNow)
                .ToListAsync();

            foreach (var group in groups)
            {
                group.IsActive = false;
                group.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Dictionary<int, List<UserGroup>>> GetExpiringUserGroups()
        {
            var expiringGroups = await _context.UserGroups
                .Where(g => g.SubscriptionEndDate < DateTime.UtcNow.AddDays(7))
                .ToListAsync();

            var expiringGroupsByDays = new Dictionary<int, List<UserGroup>>
            {
                { 1, new List<UserGroup>() },
                { 3, new List<UserGroup>() },
                { 7, new List<UserGroup>() }
            };

            foreach (var group in expiringGroups)
            {
                var daysUntilExpiration = (group.SubscriptionEndDate - DateTime.UtcNow).Days;
                if (daysUntilExpiration <= 1)
                {
                    expiringGroupsByDays[1].Add(_mapper.Map<UserGroup>(group));
                }
                else if (daysUntilExpiration <= 3)
                {
                    expiringGroupsByDays[3].Add(_mapper.Map<UserGroup>(group));
                }
                else if (daysUntilExpiration <= 7)
                {
                    expiringGroupsByDays[7].Add(_mapper.Map<UserGroup>(group));
                }
            }

            return expiringGroupsByDays;
        }
    }
}
