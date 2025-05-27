using Business.DataRepositories;
using Business.Enums;
using Business.Services.Base;
using Microsoft.Extensions.Logging;

namespace Business.Services
{
    public class LinkedUserService : ILinkedUserService
    {
        private readonly ILinkedUserRepository _linkedUserRepository;
        private readonly ILogger<LinkedUserService> _logger;

        public LinkedUserService(
            ILinkedUserRepository linkedUserRepository,
            ILogger<LinkedUserService> logger)
        {
            _linkedUserRepository = linkedUserRepository;
            _logger = logger;
        }

        public async Task<bool> IsLinkedUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("IsLinkedUserAsync called with null or empty userId");
                return false;
            }

            try
            {
                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(userId);
                return linkedUser != null && linkedUser.IsActive;
            }
            catch
            {
                _logger.LogError($"Error checking if user {userId} is a linked user");
                return false;
            }
        }

        public async Task<bool> HasPermissionAsync(string userId, LinkedUserPermissionsEnum permission)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("HasPermissionAsync called with null or empty userId");
                return false;
            }

            try
            {
                var linkedUser = await _linkedUserRepository.GetByUserIdAsync(userId);

                if (linkedUser == null || !linkedUser.IsActive)
                    return false;

                // Convert enum to int and then switch on the value
                switch ((int)permission)
                {
                    case 1: // Transaction
                        return linkedUser.CanPerformTransactions;

                    case 2: // Report
                        return linkedUser.CanGenerateReports;

                    case 4: // Product
                        return linkedUser.CanManageProducts;

                    case 8: // Stock
                        return linkedUser.CanAlterStock;

                    case 16: // Promotion
                        return linkedUser.CanManagePromotions;

                    default:
                        _logger.LogWarning($"Unknown permission check requested: {permission}");
                        return false;
                }
            }
            catch
            {
                _logger.LogError($"Error checking permissions for user {userId}");
                return false;
            }
        }
    }
}
