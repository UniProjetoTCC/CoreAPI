using Business.Models;

namespace Business.DataRepositories
{
    public interface ILinkedUserRepository
    {
        Task<LinkedUser?> GetByUserIdAsync(string userId);
        Task<LinkedUser?> GetByIdAsync(string id);
        Task<IEnumerable<LinkedUser>> GetByCreatedByUserIdAsync(string createdByUserId);
        Task<LinkedUser> CreateLinkedUserAsync(string userId, string createdByUserId, bool canPerformTransactions, bool canGenerateReports, bool canManageProducts, bool canAlterStock, bool canManagePromotions);
        Task<LinkedUser?> UpdateLinkedUserAsync(string linkedUserId, bool canPerformTransactions, bool canGenerateReports, bool canManageProducts, bool canAlterStock, bool canManagePromotions);
        Task<bool> DeleteLinkedUserAsync(string linkedUserId);
        Task<bool> IsLinkedUserAsync(string userId);
        Task<IEnumerable<LinkedUser>> GetAllByGroupIdAsync(string groupId);
        Task DeactivateLinkedUsersAsync(IEnumerable<string> linkedUserIds);
        Task ActivateLinkedUsersAsync(IEnumerable<string> linkedUserIds);
        Task<bool> DeleteAllLinkedUsersByParentIdAsync(string parentUserId);
    }
}