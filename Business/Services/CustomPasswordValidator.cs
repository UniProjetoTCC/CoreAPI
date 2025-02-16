using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business.Services.Base;

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

            // Check for obvious sequences
            if (HasObviousSequence(password))
            {
                errors.Add(new IdentityError
                {
                    Code = "SequentialPassword",
                    Description = "Password cannot contain obvious sequences (like 123, abc) or keyboard patterns."
                });
            }

            // Check if password contains user info
            if (ContainsUserInfo(user, password))
            {
                errors.Add(new IdentityError
                {
                    Code = "PersonalInfoInPassword",
                    Description = "Password cannot contain personal information (such as email or username)."
                });
            }

            // Check complexity score
            int complexityScore = CalculatePasswordComplexity(password);
            if (complexityScore < 4)
            {
                errors.Add(new IdentityError
                {
                    Code = "WeakPassword",
                    Description = "Password is too weak. Add more variety of characters and increase length."
                });
            }

            return errors.Count == 0 ? 
                await Task.FromResult(IdentityResult.Success) : 
                await Task.FromResult(IdentityResult.Failed(errors.ToArray()));
        }

        public bool HasObviousSequence(string password)
        {
            var lowerPassword = password.ToLower();

            // Check numeric sequences (ascending and descending)
            for (int i = 0; i < lowerPassword.Length - 2; i++)
            {
                if (char.IsDigit(lowerPassword[i]))
                {
                    if (i + 2 < lowerPassword.Length &&
                        char.IsDigit(lowerPassword[i + 1]) &&
                        char.IsDigit(lowerPassword[i + 2]))
                    {
                        int n1 = lowerPassword[i] - '0';
                        int n2 = lowerPassword[i + 1] - '0';
                        int n3 = lowerPassword[i + 2] - '0';
                        
                        if ((n2 == n1 + 1 && n3 == n2 + 1) || // Ascending
                            (n2 == n1 - 1 && n3 == n2 - 1))    // Descending
                        {
                            return true;
                        }
                    }
                }
            }

            // Check alphabetic sequences
            for (int i = 0; i < lowerPassword.Length - 2; i++)
            {
                if (char.IsLetter(lowerPassword[i]))
                {
                    if (i + 2 < lowerPassword.Length &&
                        char.IsLetter(lowerPassword[i + 1]) &&
                        char.IsLetter(lowerPassword[i + 2]))
                    {
                        char c1 = lowerPassword[i];
                        char c2 = lowerPassword[i + 1];
                        char c3 = lowerPassword[i + 2];
                        
                        if ((c2 == c1 + 1 && c3 == c2 + 1) || // Ascending
                            (c2 == c1 - 1 && c3 == c2 - 1))    // Descending
                        {
                            return true;
                        }
                    }
                }
            }

            // Check keyboard patterns
            string[] keyboardPatterns = {
                "qwerty", "asdfgh", "zxcvbn", // Horizontal
                "qazwsx", "wsxedc", "edcrfv", // Vertical
                "qweasd", "asdzxc", "wertyui" // Common
            };

            if (keyboardPatterns.Any(pattern => lowerPassword.Contains(pattern)))
            {
                return true;
            }

            // Check for repetitions
            for (int i = 0; i < lowerPassword.Length - 2; i++)
            {
                if (lowerPassword[i] == lowerPassword[i + 1] && 
                    lowerPassword[i] == lowerPassword[i + 2])
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsUserInfo(IdentityUser user, string password)
        {
            var passwordLower = password.ToLower();
            
            if (user.Email != null)
            {
                // Split email and check each part
                var emailParts = user.Email.ToLower().Split(new[] { '@', '.', '_', '-' });
                foreach (var part in emailParts)
                {
                    if (part.Length > 2 && passwordLower.Contains(part))
                    {
                        return true;
                    }
                }
            }

            if (user.UserName != null)
            {
                // Split username and check each part
                var usernameParts = user.UserName.ToLower().Split(new[] { '.', '_', '-', ' ' });
                foreach (var part in usernameParts)
                {
                    if (part.Length > 2 && passwordLower.Contains(part))
                    {
                        return true;
                    }
                }
            }

            return false;
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
