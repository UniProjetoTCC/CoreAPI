using Business.Models;

namespace Business.DataRepositories
{
    public interface IUserGroupRepository
    {
        Task<UserGroup?> GetByUserIdAsync(string userId);
        Task<UserGroup?> GetByGroupIdAsync(string groupId);
        Task<UserGroup> CreateGroupAsync(string userId, string subscriptionPlanId);
        Task<UserGroup?> UpdateGroupAsync(string groupId, string subscriptionPlanId);
        Task DeactivateExpiredUserGroups();
        Task<Dictionary<int, List<UserGroup>>> GetExpiringUserGroups();
    }
}