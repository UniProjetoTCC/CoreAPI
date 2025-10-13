using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Business.DataRepositories;
using Business.Services.Base;
using CoreAPI.Controllers;
using CoreAPI.Models;
using CoreAPI.UnitTests.Fixtures;
using CoreAPI.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Security.Claims;

namespace CoreAPI.UnitTests.Controllers
{
    public class UserControllerSimpleTests
    {
        // Teste simples para Register com email já existente
        [Fact]
        public async Task Register_WithExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            var fixture = new UserControllerFixture();
            var model = new RegisterModel
            {
                Username = "testuser",
                Email = "existing@example.com",
                Password = "Test@123"
            };

            var existingUser = new IdentityUser
            {
                Email = model.Email,
                UserName = "existinguser"
            };

            fixture.UserManagerMock.SetupFindByEmailAsync(model.Email, existingUser);
            
            var controller = fixture.CreateController();

            // Act
            var result = await controller.Register(model);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // Teste simples para ConfirmEmail com email inválido
        [Fact]
        public async Task ConfirmEmail_WithInvalidEmail_ReturnsBadRequest()
        {
            // Arrange
            var fixture = new UserControllerFixture();
            var email = "invalid@example.com";
            var token = "valid-token";

            fixture.UserManagerMock.SetupFindByEmailAsync(email, null);
            
            var controller = fixture.CreateController();

            // Act
            var result = await controller.ConfirmEmail(email, token);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // Teste simples para ForgotPassword
        [Fact]
        public async Task ForgotPassword_WithValidEmail_ReturnsOkResult()
        {
            // Arrange
            var fixture = new UserControllerFixture();
            var model = new ForgotPasswordModel
            {
                Email = "test@example.com"
            };

            var user = new IdentityUser
            {
                Id = "user-id",
                Email = model.Email,
                UserName = "testuser"
            };

            fixture.UserManagerMock.SetupFindByEmailAsync(model.Email, user);
            fixture.UserManagerMock.SetupGeneratePasswordResetTokenAsync(user, "reset-token");

            fixture.EmailSenderMock.Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var controller = fixture.CreateController();

            // Act
            var result = await controller.ForgotPassword(model);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        // Teste simples para ResetPassword
        [Fact]
        public async Task ResetPassword_WithValidData_ReturnsOkResult()
        {
            // Arrange
            var fixture = new UserControllerFixture();
            var model = new ResetPasswordModel
            {
                Email = "test@example.com",
                Token = "valid-token",
                NewPassword = "NewPassword@123"
            };

            var user = new IdentityUser
            {
                Id = "user-id",
                Email = model.Email,
                UserName = "testuser"
            };

            fixture.UserManagerMock.SetupFindByEmailAsync(model.Email, user);
            fixture.UserManagerMock.SetupResetPasswordAsync(user, model.Token, model.NewPassword, IdentityResult.Success);

            var controller = fixture.CreateController();

            // Act
            var result = await controller.ResetPassword(model);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
