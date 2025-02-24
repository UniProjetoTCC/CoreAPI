using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business.Models;

namespace Business.DataRepositories
{
    public interface IUserGroupRepository
    {
        Task<UserGroup?> GetByUserIdAsync(string userId);
        Task<UserGroup?> GetByGroupIdAsync(int groupId);
        Task<UserGroup> CreateGroupAsync(string userId, int subscriptionPlanId);
        Task<UserGroup?> UpdateGroupAsync(int groupId, int subscriptionPlanId);
        Task DeactivateExpiredUserGroups();
        Task<Dictionary<int, List<UserGroup>>> GetExpiringUserGroups();
    }
}