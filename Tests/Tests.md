# CoreAPI Testing Guide

This comprehensive guide provides detailed instructions on how to create effective tests for the CoreAPI project, with a focus on fixtures, helpers, and best practices for achieving high-quality test coverage.

## Table of Contents

- [Test Structure](#test-structure)
- [Creating Fixtures](#creating-fixtures)
- [Creating Helpers](#creating-helpers)
- [Mocking Dependencies](#mocking-dependencies)
- [Test Data Generation](#test-data-generation)
- [Code Coverage](#code-coverage)
- [Best Practices](#best-practices)
- [Advanced Testing Scenarios](#advanced-testing-scenarios)

## Test Structure

The test projects follow a clear organizational structure that mirrors the architecture of the main application:

```
Tests/
├── CoreAPI.UnitTests/           # Tests for API controllers and components
│   ├── Controllers/             # Tests for API controllers
│   ├── Helpers/                 # Test helper classes
│   └── Fixtures/                # Test fixtures and setup code
│
├── Business.UnitTests/          # Tests for business logic layer
│   ├── Services/                # Tests for service implementations
│   ├── DataRepositories/        # Tests for repository interfaces
│   ├── Helpers/                 # Test helper classes
│   └── Fixtures/                # Test fixtures and setup code
│
├── Data.UnitTests/              # Tests for data access layer
│   ├── Repositories/            # Tests for repository implementations
│   ├── Context/                 # Tests for database context
│   └── Fixtures/                # Test fixtures and setup code
│
└── CoreAPI.IntegrationTests/    # End-to-end integration tests
    ├── Controllers/             # API endpoint integration tests
    └── Fixtures/                # Integration test fixtures
```

Each test project should mirror the structure of the project it's testing to maintain clarity and organization.

## Creating Fixtures

Fixtures are reusable test contexts that provide a consistent environment for your tests. They eliminate repetitive setup code, ensure test isolation, and make tests more maintainable. Fixtures are particularly valuable for integration tests and scenarios requiring complex setup.

### Database Fixtures

For tests that require a database, create a fixture that sets up an in-memory database:

```csharp
// Example: Tests/Data.UnitTests/Fixtures/DatabaseFixture.cs
public class DatabaseFixture : IDisposable
{
    public ApplicationDbContext DbContext { get; }

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        DbContext = new ApplicationDbContext(options);
        
        // Seed the database with test data
        SeedDatabase();
    }

    private void SeedDatabase()
    {
        // Add test data
        DbContext.Users.Add(new User { Id = Guid.NewGuid(), Username = "testuser" });
        DbContext.SaveChanges();
    }

    public void Dispose()
    {
        DbContext.Dispose();
    }
}
```

### Web Application Factory for Integration Tests

For API integration tests, create a custom `WebApplicationFactory`:

```csharp
// Example: Tests/CoreAPI.IntegrationTests/Fixtures/TestWebApplicationFactory.cs
public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace services with test implementations
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestsDb");
            });

            // Replace other services as needed
            services.AddScoped<IEmailService, FakeEmailService>();

            // Build and seed the database
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
                SeedTestData(db);
            }
        });
    }

    private void SeedTestData(ApplicationDbContext context)
    {
        // Add test data for integration tests
        context.Users.Add(new User { Id = Guid.NewGuid(), Username = "integrationtestuser" });
        context.SaveChanges();
    }
}
```

### Using Fixtures in Tests

xUnit provides several ways to use fixtures in your tests:

#### Class Fixtures

Use the `IClassFixture<T>` interface to share a fixture across all tests in a class. The fixture is created once before any tests in the class run and disposed after all tests complete:

```csharp
public class UserRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUserById_ReturnsUser()
    {
        // Arrange
        var repository = new UserRepository(_fixture.DbContext);

        // Act & Assert
        // Use the repository with the fixture's DbContext
    }
}
```

For integration tests:

```csharp
public class UserControllerTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UserControllerTests(TestWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
```

## Creating Helpers

Helpers are utility classes that encapsulate common functionality needed across multiple tests. They improve test readability, reduce duplication, and make tests more maintainable. Well-designed helpers focus on a single responsibility and provide clear, intuitive interfaces.

### Authentication Helper

For testing authenticated endpoints:

```csharp
// Example: Tests/CoreAPI.IntegrationTests/Helpers/AuthenticationHelper.cs
public static class AuthenticationHelper
{
    public static async Task<string> GetJwtTokenAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = username,
            Password = password
        });

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return content.Token;
    }

    public static HttpClient CreateClientWithAuthentication(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
```

### Test Data Generator

Create a helper to generate test data:

```csharp
// Example: Tests/Business.UnitTests/Helpers/TestDataGenerator.cs
public static class TestDataGenerator
{
    public static User CreateTestUser(string username = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username ?? $"user_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
            Email = $"{Guid.NewGuid().ToString("N").Substring(0, 8)}@example.com",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static List<User> CreateTestUsers(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateTestUser())
            .ToList();
    }
}
```

## Mocking Dependencies

Mocking is essential for isolating the code under test from its dependencies. This ensures you're testing only the target component's behavior, not its dependencies. The CoreAPI project uses Moq, a popular mocking framework for .NET.

### Basic Mocking Patterns

Use Moq to create and configure mock objects for dependencies in unit tests:

```csharp
// Example: Setting up a mock repository
var repositoryMock = new Mock<IUserRepository>();
repositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>()))
    .ReturnsAsync((Guid id) => new User { Id = id, Username = "testuser" });

// Example: Verifying interactions
repositoryMock.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
```

### Common Mocking Patterns

#### Mocking HTTP Responses

```csharp
var handlerMock = new Mock<HttpMessageHandler>();
handlerMock.Protected()
    .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>()
    )
    .ReturnsAsync(new HttpResponseMessage
    {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent("{\"data\":\"test\"}")
    });

var httpClient = new HttpClient(handlerMock.Object);
```

#### Mocking DbContext

```csharp
var dbContextMock = new Mock<ApplicationDbContext>();
var dbSetMock = MockDbSet(testUsers);
dbContextMock.Setup(c => c.Users).Returns(dbSetMock.Object);

// Helper method to mock DbSet
private static Mock<DbSet<T>> MockDbSet<T>(List<T> data) where T : class
{
    var queryable = data.AsQueryable();
    var dbSetMock = new Mock<DbSet<T>>();
    
    dbSetMock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
    dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
    dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
    dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
    
    return dbSetMock;
}
```

## Test Data Generation

Creating realistic, varied test data is crucial for thorough testing. Manual test data creation is tedious and often leads to repetitive, non-diverse test cases. The following libraries can help generate rich test data efficiently.

### Using AutoFixture

AutoFixture is a powerful library that automatically generates test objects, helping you focus on the test logic rather than test data setup:

AutoFixture is a library that helps create test data:

1. Add the package:

```bash
dotnet add package AutoFixture
```

2. Use it in tests:

```csharp
var fixture = new Fixture();
var user = fixture.Create<User>();
var users = fixture.CreateMany<User>(10).ToList();
```

### Using Bogus

Bogus is a library for generating realistic test data:

1. Add the package:

```bash
dotnet add package Bogus
```

2. Use it in tests:

```csharp
var faker = new Faker<User>()
    .RuleFor(u => u.Id, f => Guid.NewGuid())
    .RuleFor(u => u.Username, f => f.Internet.UserName())
    .RuleFor(u => u.Email, f => f.Internet.Email())
    .RuleFor(u => u.CreatedAt, f => f.Date.Past());

var user = faker.Generate();
var users = faker.Generate(10);
```

## Code Coverage

Code coverage measures how much of your codebase is executed during tests. While high coverage doesn't guarantee bug-free code, it helps identify untested areas. Aim for comprehensive coverage of critical business logic and edge cases.

### Running Tests with Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### Generating Coverage Reports

1. Install ReportGenerator:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

2. Generate HTML report:

```bash
reportgenerator "-reports:Tests/**/coverage.cobertura.xml" "-targetdir:TestResults/CoverageReport" "-reporttypes:Html"
```

3. View the report at `TestResults/CoverageReport/index.html`

### Coverage Thresholds

You can set coverage thresholds in your project file:

```xml
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>cobertura</CoverletOutputFormat>
  <Threshold>80</Threshold>
  <ThresholdType>line,branch,method</ThresholdType>
</PropertyGroup>
```

## Best Practices

### Naming Conventions

Consistent, descriptive naming makes tests easier to understand and maintain:

- **Test classes**: `{ClassUnderTest}Tests`  
  Example: `UserServiceTests`, `AuthControllerTests`

- **Test methods**: `{MethodUnderTest}_{Scenario}_{ExpectedResult}`  
  Example: `GetUser_WithValidId_ReturnsUser`, `Login_WithInvalidCredentials_ReturnsBadRequest`

### Test Organization

Well-organized tests are easier to understand, maintain, and debug:

- **AAA Pattern**: Structure tests with clear Arrange, Act, and Assert sections
  ```csharp
  [Fact]
  public async Task GetUser_WithValidId_ReturnsUser()
  {
      // Arrange - Set up test prerequisites
      var userId = Guid.NewGuid();
      _repositoryMock.Setup(r => r.GetByIdAsync(userId))
          .ReturnsAsync(new User { Id = userId, Name = "Test User" });

      // Act - Execute the code being tested
      var result = await _userService.GetUserAsync(userId);

      // Assert - Verify the outcome
      result.Should().NotBeNull();
      result.Id.Should().Be(userId);
  }
  ```

- **Single Responsibility**: Each test should verify one specific behavior
- **Descriptive Names**: Test names should clearly explain the scenario and expected outcome

### Test Independence

Independent tests are more reliable and easier to maintain:

- **Isolation**: Tests should not depend on each other's execution or order
- **Self-contained**: Each test should set up its own test data and environment
- **Clean up**: Tests should clean up any resources they create
- **Idempotent**: Tests should produce the same results when run multiple times

### Testing Edge Cases

Comprehensive testing requires covering various scenarios:

- **Happy Paths**: Test the expected, normal flow
- **Error Conditions**: Test how the system handles errors and exceptions
- **Edge Cases**: Test with empty collections, null values, maximum/minimum values
- **Boundary Conditions**: Test at the boundaries of valid input ranges
- **Concurrency**: Test behavior under concurrent access (where applicable)

### Continuous Integration

- Run tests on every pull request
- Enforce code coverage thresholds
- Generate and publish coverage reports

### Example: Complete Test Class

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _service = new UserService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserById_WithValidId_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new User { Id = userId, Username = "testuser" };
        _repositoryMock.Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedUser);
        _repositoryMock.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserById_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repositoryMock.Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync((User)null);

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreatedUser()
    {
        // Arrange
        var newUser = new User { Username = "newuser", Email = "new@example.com" };
        var createdUser = new User { Id = Guid.NewGuid(), Username = "newuser", Email = "new@example.com" };
        
        _repositoryMock.Setup(repo => repo.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _service.CreateUserAsync(newUser);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Username.Should().Be(newUser.Username);
        _repositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<User>()), Times.Once);
    }
}
```

## Advanced Testing Scenarios

### Testing Asynchronous Code

```csharp
[Fact]
 public async Task ProcessDataAsync_CompletesSuccessfully()
{
    // Arrange
    var data = new ProcessData { /* ... */ };
    _serviceMock.Setup(s => s.ProcessAsync(data))
        .Returns(Task.CompletedTask);

    // Act
    await _processor.ProcessDataAsync(data);

    // Assert
    _serviceMock.Verify(s => s.ProcessAsync(data), Times.Once);
}
```

### Testing with In-Memory Database

```csharp
[Fact]
 public async Task GetUsers_ReturnsAllUsers()
{
    // Arrange
    using var context = new ApplicationDbContext(_dbContextOptions);
    var users = new List<User>
    {
        new User { Id = Guid.NewGuid(), Name = "User 1" },
        new User { Id = Guid.NewGuid(), Name = "User 2" }
    };
    await context.Users.AddRangeAsync(users);
    await context.SaveChangesAsync();

    var repository = new UserRepository(context);

    // Act
    var result = await repository.GetAllAsync();

    // Assert
    result.Should().HaveCount(2);
    result.Should().BeEquivalentTo(users);
}
```

### Testing Authorization

```csharp
[Fact]
 public async Task GetSecureResource_WithoutAuthorization_ReturnsForbidden()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/secure-resource");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

By following these guidelines and examples, you'll be able to create comprehensive, maintainable tests that ensure your code works as expected across all scenarios.
