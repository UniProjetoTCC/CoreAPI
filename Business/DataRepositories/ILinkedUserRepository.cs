using System.Collections.Generic;
using System.Threading.Tasks;
using Business.Models;

namespace Business.DataRepositories
{
    public interface ILinkedUserRepository
    {
        Task<LinkedUser?> GetByUserIdAsync(string userId);
        Task<LinkedUser?> GetByIdAsync(int id);
        Task<IEnumerable<LinkedUser>> GetByCreatedByUserIdAsync(string createdByUserId);
        Task<LinkedUser> CreateLinkedUserAsync(string userId, string createdByUserId, bool canPerformTransactions, bool canGenerateReports, bool canManageProducts, bool canAlterStock, bool canManagePromotions);
        Task<LinkedUser?> UpdateLinkedUserAsync(string id, bool canPerformTransactions, bool canGenerateReports, bool canManageProducts, bool canAlterStock, bool canManagePromotions);
        Task<bool> DeleteLinkedUserAsync(string id);
        Task<IEnumerable<LinkedUser>> GetAllByGroupIdAsync(int groupId);
        Task DeactivateLinkedUsersAsync(IEnumerable<int> linkedUserIds);
        Task ActivateLinkedUsersAsync(IEnumerable<int> linkedUserIds);
    }
}