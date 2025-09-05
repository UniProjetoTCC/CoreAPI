using Business.Models;

namespace Business.Services.Base
{
    public interface ILoyaltyPointsService
    {
        Task<(CustomerBusinessModel Customer, int PointsAdded, decimal ConversionRate)> AddPointsAsync(
            string customerId, 
            decimal amount, 
            string groupId, 
            string? description = null);

        Task<(CustomerBusinessModel Customer, int PointsRemoved, decimal ConversionRate)> RemovePointsAsync(
            string customerId, 
            decimal amount, 
            string groupId, 
            string? description = null);

        Task<(CustomerBusinessModel Customer, int PointsRemoved, decimal ConversionRate)> AdminRemovePointsAsync(
            string customerId, 
            int points, 
            string groupId, 
            string description);

        Task<(CustomerBusinessModel Customer, decimal ConversionRate)> GetPointsBalanceAsync(
            string customerId, 
            string groupId);
    }
}
