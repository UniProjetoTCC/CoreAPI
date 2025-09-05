using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Business.DataRepositories;
using Business.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Business.UnitTests.Services
{
    public class ExampleServiceTests
    {
        private readonly Mock<ILogger<ExampleService>> _loggerMock;
        private readonly Mock<IExampleRepository> _repositoryMock;
        private readonly ExampleService _service;

        public ExampleServiceTests()
        {
            // Arrange - Setup mocks
            _loggerMock = new Mock<ILogger<ExampleService>>();
            _repositoryMock = new Mock<IExampleRepository>();
            
            // Create service with mocked dependencies
            _service = new ExampleService(_loggerMock.Object, _repositoryMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllItems()
        {
            // Arrange
            var expectedItems = new List<object> { new object(), new object() };
            _repositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(expectedItems);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().BeEquivalentTo(expectedItems);
            
            // Verify repository was called
            _repositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsItem()
        {
            // Arrange
            var id = Guid.NewGuid();
            var expectedItem = new object();
            _repositoryMock.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(expectedItem);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            result.Should().Be(expectedItem);
            
            // Verify repository was called with correct parameter
            _repositoryMock.Verify(x => x.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync((object)null);

            // Act
            var result = await _service.GetByIdAsync(id);

            // Assert
            result.Should().BeNull();
            
            // Verify repository was called with correct parameter
            _repositoryMock.Verify(x => x.GetByIdAsync(id), Times.Once);
        }
        
        [Fact]
        public async Task CreateAsync_WithValidItem_ReturnsCreatedItem()
        {
            // Arrange
            var item = new object();
            _repositoryMock.Setup(x => x.CreateAsync(item))
                .ReturnsAsync(item);

            // Act
            var result = await _service.CreateAsync(item);

            // Assert
            result.Should().Be(item);
            
            // Verify repository was called with correct parameter
            _repositoryMock.Verify(x => x.CreateAsync(item), Times.Once);
        }
    }
}
