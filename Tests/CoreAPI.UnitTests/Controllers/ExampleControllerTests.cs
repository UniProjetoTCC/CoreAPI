using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreAPI.Controllers;
using Business.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace CoreAPI.UnitTests.Controllers
{
    public class ExampleControllerTests
    {
        private readonly Mock<ILogger<ExampleController>> _loggerMock;
        private readonly Mock<IExampleService> _serviceMock;
        private readonly ExampleController _controller;

        public ExampleControllerTests()
        {
            // Arrange - Setup mocks
            _loggerMock = new Mock<ILogger<ExampleController>>();
            _serviceMock = new Mock<IExampleService>();
            
            // Create controller with mocked dependencies
            _controller = new ExampleController(_loggerMock.Object, _serviceMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithListOfItems()
        {
            // Arrange
            var expectedItems = new List<object> { new object(), new object() };
            _serviceMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(expectedItems);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var items = okResult.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject;
            items.Should().BeEquivalentTo(expectedItems);
            
            // Verify service was called
            _serviceMock.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var expectedItem = new object();
            _serviceMock.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(expectedItem);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedItem);
            
            // Verify service was called with correct parameter
            _serviceMock.Verify(x => x.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _serviceMock.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync((object)null);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
            
            // Verify service was called with correct parameter
            _serviceMock.Verify(x => x.GetByIdAsync(id), Times.Once);
        }
    }
}
