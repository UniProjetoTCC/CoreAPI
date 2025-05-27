namespace Business.Services.Base
{
    public interface ILinkedUserService
    {
        Task<bool> IsLinkedUserAsync(string userId);

        Task<bool> HasPermissionAsync(string userId, Business.Enums.LinkedUserPermissionsEnum permission);
    }
}
