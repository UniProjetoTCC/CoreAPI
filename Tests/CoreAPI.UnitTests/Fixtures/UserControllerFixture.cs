using System;
using System.Collections.Generic;
using System.Security.Claims;
using Business.DataRepositories;
using Business.Services.Base;
using CoreAPI.Controllers;
using CoreAPI.UnitTests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

#nullable disable

namespace CoreAPI.UnitTests.Fixtures
{
    public class UserControllerFixture
    {
        public Mock<UserManager<IdentityUser>> UserManagerMock { get; }
        public Mock<SignInManager<IdentityUser>> SignInManagerMock { get; }
        public Mock<IRoleService> RoleServiceMock { get; }
        public Mock<IUserGroupRepository> UserGroupRepositoryMock { get; }
        public Mock<ISubscriptionPlanRepository> SubscriptionPlanRepositoryMock { get; }
        public Mock<RoleManager<IdentityRole>> RoleManagerMock { get; }
        public Mock<IConfiguration> ConfigurationMock { get; }
        public Mock<IEmailSenderService> EmailSenderMock { get; }
        public Mock<ILogger<UserController>> LoggerMock { get; }
        public Mock<ILinkedUserRepository> LinkedUserRepositoryMock { get; }
        public Mock<IBackgroundJobService> BackgroundJobServiceMock { get; }

        public UserControllerFixture()
        {
            // Setup UserManager mock
            UserManagerMock = IdentityHelpers.MockUserManager<IdentityUser>();

            // Setup SignInManager mock
            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
            SignInManagerMock = new Mock<SignInManager<IdentityUser>>(
                UserManagerMock.Object,
                contextAccessorMock.Object,
                userPrincipalFactoryMock.Object,
                It.IsAny<IOptions<IdentityOptions>>(),
                It.IsAny<ILogger<SignInManager<IdentityUser>>>(),
                It.IsAny<IAuthenticationSchemeProvider>(),
                It.IsAny<IUserConfirmation<IdentityUser>>());

            // Setup RoleManager mock
            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
            RoleManagerMock = new Mock<RoleManager<IdentityRole>>(
                roleStoreMock.Object, 
                It.IsAny<IEnumerable<IRoleValidator<IdentityRole>>>(),
                It.IsAny<ILookupNormalizer>(),
                It.IsAny<IdentityErrorDescriber>(),
                It.IsAny<ILogger<RoleManager<IdentityRole>>>());

            // Setup other mocks
            RoleServiceMock = new Mock<IRoleService>();
            UserGroupRepositoryMock = new Mock<IUserGroupRepository>();
            SubscriptionPlanRepositoryMock = new Mock<ISubscriptionPlanRepository>();
            ConfigurationMock = new Mock<IConfiguration>();
            EmailSenderMock = new Mock<IEmailSenderService>();
            LoggerMock = new Mock<ILogger<UserController>>();
            LinkedUserRepositoryMock = new Mock<ILinkedUserRepository>();
            BackgroundJobServiceMock = new Mock<IBackgroundJobService>();

            // Setup default configuration
            ConfigurationMock.Setup(x => x["ProjectName"]).Returns("Test Project");
        }

        public UserController CreateController()
        {
            return new UserController(
                UserManagerMock.Object,
                SignInManagerMock.Object,
                RoleServiceMock.Object,
                UserGroupRepositoryMock.Object,
                SubscriptionPlanRepositoryMock.Object,
                RoleManagerMock.Object,
                ConfigurationMock.Object,
                EmailSenderMock.Object,
                LoggerMock.Object,
                LinkedUserRepositoryMock.Object,
                BackgroundJobServiceMock.Object);
        }
    }
}
