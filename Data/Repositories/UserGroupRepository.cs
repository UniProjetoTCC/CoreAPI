using System;
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

        public async Task<UserGroup?> GetByGroupIdAsync(int groupId)
        {   
            var group = await _context.UserGroups
                .Include(g => g.SubscriptionPlan)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null) return null;

            return _mapper.Map<UserGroup>(group);
        }

        public async Task<UserGroup> CreateGroupAsync(string userId, int subscriptionPlanId)
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

        public async Task<UserGroup?> UpdateGroupAsync(int groupId, int subscriptionPlanId)
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
    }
}
