using Business.Services.Base;
using Microsoft.AspNetCore.Identity;

namespace Business.Services
{
    public class CustomPasswordValidator : ICustomPasswordValidator, IPasswordValidator<IdentityUser>
    {
        public async Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user, string? password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return await Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Code = "EmptyPassword",
                    Description = "Password cannot be empty"
                }));
            }

            var errors = new List<IdentityError>();

            // Check for obvious sequences with context
            var seq = GetObviousSequence(password);
            if (!string.IsNullOrEmpty(seq))
            {
                errors.Add(new IdentityError { Code = "SequentialPassword", Description = $"Password cannot contain obvious sequence '{seq}'." });
            }

            // Check if password contains user info
            var personal = GetPersonalInfoSubstring(user, password);
            if (!string.IsNullOrEmpty(personal))
            {
                errors.Add(new IdentityError { Code = "PersonalInfoInPassword", Description = $"Password cannot contain personal information '{personal}'." });
            }

            // Validações de requisitos de complexidade individuais
            if (password.Length < 10)
                errors.Add(new IdentityError { Code = "PasswordTooShort", Description = "Password must be at least 10 characters long." });
            if (!password.Any(char.IsUpper))
                errors.Add(new IdentityError { Code = "PasswordRequiresUpper", Description = "Password must contain at least one uppercase letter." });
            if (!password.Any(char.IsLower))
                errors.Add(new IdentityError { Code = "PasswordRequiresLower", Description = "Password must contain at least one lowercase letter." });
            if (!password.Any(char.IsDigit))
                errors.Add(new IdentityError { Code = "PasswordRequiresDigit", Description = "Password must contain at least one digit." });
            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                errors.Add(new IdentityError { Code = "PasswordRequiresSpecial", Description = "Password must contain at least one special character." });

            return errors.Count == 0 ?
                await Task.FromResult(IdentityResult.Success) :
                await Task.FromResult(IdentityResult.Failed(errors.ToArray()));
        }

        public string? GetObviousSequence(string password)
        {
            var lower = password.ToLower();

            for (int i = 0; i < lower.Length - 3; i++)
            {
                if (char.IsDigit(lower[i]) && char.IsDigit(lower[i + 1]) && char.IsDigit(lower[i + 2]) && char.IsDigit(lower[i + 3]))
                {
                    int n1 = lower[i] - '0', n2 = lower[i + 1] - '0', n3 = lower[i + 2] - '0', n4 = lower[i + 3] - '0';
                    if ((n2 == n1 + 1 && n3 == n2 + 1 && n4 == n3 + 1) || (n2 == n1 - 1 && n3 == n2 - 1 && n4 == n3 - 1))
                        return password.Substring(i, 4);
                }
            }

            for (int i = 0; i < lower.Length - 3; i++)
            {
                if (char.IsLetter(lower[i]) && char.IsLetter(lower[i + 1]) && char.IsLetter(lower[i + 2]) && char.IsLetter(lower[i + 3]))
                {
                    char c1 = lower[i], c2 = lower[i + 1], c3 = lower[i + 2], c4 = lower[i + 3];
                    if ((c2 == c1 + 1 && c3 == c2 + 1 && c4 == c3 + 1) || (c2 == c1 - 1 && c3 == c2 - 1 && c4 == c3 - 1))
                        return password.Substring(i, 4);
                }
            }

            string[] patterns = { "qwerty", "asdfgh", "zxcvbn", "qazwsx", "wsxedc", "edcrfv", "qweasd", "asdzxc", "wertyui" };
            foreach (var p in patterns) if (lower.Contains(p)) return p;

            for (int i = 0; i < lower.Length - 2; i++) if (lower[i] == lower[i + 1] && lower[i] == lower[i + 2]) return new string(lower[i], 3);
            return null;
        }

        public string? GetPersonalInfoSubstring(IdentityUser user, string password)
        {
            var lower = password.ToLower();
            if (user.Email != null)
            {
                var parts = user.Email.ToLower().Split(new[] { '@', '.', '_', '-' });
                foreach (var part in parts) if (part.Length > 2 && lower.Contains(part)) return part;
            }
            if (user.UserName != null)
            {
                var parts = user.UserName.ToLower().Split(new[] { '.', '_', '-', ' ' });
                foreach (var part in parts) if (part.Length > 2 && lower.Contains(part)) return part;
            }
            return null;
        }

        public int CalculatePasswordComplexity(string password)
        {
            int score = 0;

            // Length
            if (password.Length >= 10) score++;
            if (password.Length >= 12) score++;
            if (password.Length >= 16) score++;

            // Uppercase characters
            int uppercaseCount = password.Count(char.IsUpper);
            if (uppercaseCount >= 1) score++;
            if (uppercaseCount >= 3) score++;

            // Lowercase characters
            int lowercaseCount = password.Count(char.IsLower);
            if (lowercaseCount >= 1) score++;
            if (lowercaseCount >= 3) score++;

            // Numbers
            int digitCount = password.Count(char.IsDigit);
            if (digitCount >= 1) score++;
            if (digitCount >= 3) score++;

            // Special characters
            int specialCount = password.Count(ch => !char.IsLetterOrDigit(ch));
            if (specialCount >= 1) score++;
            if (specialCount >= 2) score++;

            // Character variety
            var uniqueChars = password.Distinct().Count();
            if (uniqueChars >= 8) score++;
            if (uniqueChars >= 12) score++;

            // Character distribution
            bool hasGoodDistribution = true;
            for (int i = 0; i < password.Length - 2; i++)
            {
                if (password[i] == password[i + 1] && password[i] == password[i + 2])
                {
                    hasGoodDistribution = false;
                    break;
                }
            }
            if (hasGoodDistribution) score++;

            return score;
        }
    }
}
