using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Business.Services.Base
{
    public interface ICustomPasswordValidator
    {
        Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user, string password);
        
        bool HasObviousSequence(string password);
        
        bool ContainsUserInfo(IdentityUser user, string password);
        
        int CalculatePasswordComplexity(string password);
    }
}
