using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CoreAPI.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CoreAPI.IntegrationTests.Controllers
{
    public class ExampleControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ExampleControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            // Configure test server with custom services if needed
            _client = factory
                .WithWebHostBuilder(builder =>
                {
                    // You can configure test services here
                    // builder.ConfigureServices(services => { ... });
                })
                .CreateClient();
        }

        [Fact]
        public async Task GetAll_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/example");

            // Assert
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsSuccessStatusCode()
        {
            // Arrange
            var id = Guid.NewGuid(); // You might need to use a known ID that exists in your test database

            // Act
            var response = await _client.GetAsync($"/api/example/{id}");

            // Assert
            // This might fail if the ID doesn't exist, adjust as needed for your test environment
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/example/{invalidId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_WithValidData_ReturnsCreatedStatusCode()
        {
            // Arrange
            var newItem = new
            {
                Name = "Test Item",
                Description = "Test Description"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/example", newItem);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();
        }
    }
}
