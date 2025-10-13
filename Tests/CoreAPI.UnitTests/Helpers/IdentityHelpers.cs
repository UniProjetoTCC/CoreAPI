using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

#nullable disable

namespace CoreAPI.UnitTests.Helpers
{
    public static class IdentityHelpers
    {
        public static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var mgr = new Mock<UserManager<TUser>>(
                store.Object, 
                It.IsAny<IOptions<IdentityOptions>>(),
                It.IsAny<IPasswordHasher<TUser>>(),
                It.IsAny<IEnumerable<IUserValidator<TUser>>>(),
                It.IsAny<IEnumerable<IPasswordValidator<TUser>>>(),
                It.IsAny<ILookupNormalizer>(),
                It.IsAny<IdentityErrorDescriber>(),
                It.IsAny<IServiceProvider>(),
                It.IsAny<ILogger<UserManager<TUser>>>());
            mgr.Object.UserValidators.Add(new UserValidator<TUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());
            return mgr;
        }

        public static void SetupFindByEmailAsync<TUser>(this Mock<UserManager<TUser>> userManagerMock, string email, TUser user) where TUser : class
        {
            userManagerMock.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(user);
        }

        public static void SetupCreateAsync<TUser>(this Mock<UserManager<TUser>> userManagerMock, TUser user, string password, IdentityResult result) where TUser : class
        {
            userManagerMock.Setup(x => x.CreateAsync(user, password))
                .ReturnsAsync(result);
        }

        public static void SetupGenerateEmailConfirmationTokenAsync<TUser>(this Mock<UserManager<TUser>> userManagerMock, TUser user, string token) where TUser : class
        {
            userManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
                .ReturnsAsync(token);
        }

        public static void SetupConfirmEmailAsync<TUser>(this Mock<UserManager<TUser>> userManagerMock, TUser user, string token, IdentityResult result) where TUser : class
        {
            userManagerMock.Setup(x => x.ConfirmEmailAsync(user, token))
                .ReturnsAsync(result);
        }

        public static void SetupGeneratePasswordResetTokenAsync<TUser>(this Mock<UserManager<TUser>> userManagerMock, TUser user, string token) where TUser : class
        {
            userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync(token);
        }

        public static void SetupResetPasswordAsync<TUser>(this Mock<UserManager<TUser>> userManagerMock, TUser user, string token, string newPassword, IdentityResult result) where TUser : class
        {
            userManagerMock.Setup(x => x.ResetPasswordAsync(user, token, newPassword))
                .ReturnsAsync(result);
        }
    }
}
