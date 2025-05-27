namespace Business.Services.Base
{
    public interface IRoleService
    {
        Task SetupRolesAndPlansAsync();
        Task<bool> AssignRoleAsync(string userId, string roleName);
        Task<bool> AssignLinkedUserAsync(string userId, string createdByUserId, LinkedUserPermissions permissions);
        Task<string?> GetUserRoleAsync(string userId);
        Task<bool> CreateLinkedUserAsync(string createdByUserId, LinkedUserPermissions permissions);
        Task<bool> UpdateLinkedUserAsync(string userId, LinkedUserPermissions permissions);
        Task<bool> DeleteLinkedUserAsync(string userId);

    }



    public class LinkedUserPermissions
    {
        public bool CanPerformTransactions { get; set; }
        public bool CanGenerateReports { get; set; }
        public bool CanManageProducts { get; set; }
        public bool CanAlterStock { get; set; }
        public bool CanManagePromotions { get; set; }
    }
}