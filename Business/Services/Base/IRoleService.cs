using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.Base
{
    public interface IRoleService
    {
        Task SetupRolesAndPlansAsync();
        Task<bool> AssignRoleAsync(string userId, string roleName);
        Task<bool> AssignLinkedUserAsync(string userId, string createdByUserId, LinkedUserPermissions permissions);
        Task<string?> GetUserRoleAsync(string userId);
        
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