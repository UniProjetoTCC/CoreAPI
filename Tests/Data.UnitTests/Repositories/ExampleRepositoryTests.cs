using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Context;
using Data.Models;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using FluentAssertions;

namespace Data.UnitTests.Repositories
{
    public class ExampleRepositoryTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;

        public ExampleRepositoryTests()
        {
            // Setup in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"ExampleDb_{Guid.NewGuid()}")
                .Options;
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllItems()
        {
            // Arrange
            var entities = new List<ExampleEntity>
            {
                new ExampleEntity { Id = Guid.NewGuid(), Name = "Test 1" },
                new ExampleEntity { Id = Guid.NewGuid(), Name = "Test 2" }
            };

            // Seed the in-memory database
            using (var context = new ApplicationDbContext(_dbContextOptions))
            {
                await context.ExampleEntities.AddRangeAsync(entities);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new ApplicationDbContext(_dbContextOptions))
            {
                var repository = new ExampleRepository(context);
                var result = await repository.GetAllAsync();

                // Assert
                result.Should().HaveCount(2);
                result.Should().BeEquivalentTo(entities);
            }
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsItem()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var entity = new ExampleEntity { Id = entityId, Name = "Test" };

            // Seed the in-memory database
            using (var context = new ApplicationDbContext(_dbContextOptions))
            {
                await context.ExampleEntities.AddAsync(entity);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new ApplicationDbContext(_dbContextOptions))
            {
                var repository = new ExampleRepository(context);
                var result = await repository.GetByIdAsync(entityId);

                // Assert
                result.Should().NotBeNull();
                result.Id.Should().Be(entityId);
                result.Name.Should().Be("Test");
            }
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            using (var context = new ApplicationDbContext(_dbContextOptions))
            {
                var repository = new ExampleRepository(context);
                var result = await repository.GetByIdAsync(invalidId);

                // Assert
                result.Should().BeNull();
            }
        }

        [Fact]
        public async Task CreateAsync_AddsEntityToDatabase()
        {
            // Arrange
            var entity = new ExampleEntity { Id = Guid.NewGuid(), Name = "New Test" };

            // Act
            using (var context = new ApplicationDbContext(_dbContextOptions))
            {
                var repository = new ExampleRepository(context);
                var result = await repository.CreateAsync(entity);

                // Assert
                result.Should().NotBeNull();
                result.Id.Should().Be(entity.Id);
                result.Name.Should().Be("New Test");
            }

            // Verify entity was added to database
            using (var context = new ApplicationDbContext(_dbContextOptions))
            {
                var savedEntity = await context.ExampleEntities.FindAsync(entity.Id);
                savedEntity.Should().NotBeNull();
                savedEntity.Name.Should().Be("New Test");
            }
        }
    }
}
