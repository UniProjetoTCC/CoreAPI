using Microsoft.AspNetCore.Identity;

namespace Business.Services.Base
{
    public interface ICustomPasswordValidator
    {
        Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user, string? password);

        // Métodos auxiliares para detalhes de validação
        string? GetObviousSequence(string password);
        string? GetPersonalInfoSubstring(IdentityUser user, string password);

        int CalculatePasswordComplexity(string password);
    }
}
