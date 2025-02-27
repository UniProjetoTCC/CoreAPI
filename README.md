# CoreAPI - .NET Web API

A .NET 8.0 Web API built with Docker, PostgreSQL, and various advanced services for distributed processing and resource management.

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Environment Setup](#environment-setup)
3. [Project Structure](#project-structure)
4. [API Documentation](#api-documentation)
5. [Available Services](#available-services)
   - [Redis Cache](#redis-cache)
   - [Message Broker](#redis-message-broker)
   - [Rate Limiting](#ip-rate-limiting)
   - [Background Jobs](#background-jobs-hangfire)

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) (optional)
- [VS Code](https://code.visualstudio.com/) (optional)

## Environment Setup

### 1. Environment File (.env)
```env
DB_HOST=INSERT_YOUR_DB_HOST_HERE
DB_PORT=5432
DB_NAME=CoreAPI-DB
DB_USER=INSERT_YOUR_DB_USER_HERES
DB_PASSWORD=INSERT_YOUR_DB_PASSWORD_HERE
POSTGRES_PASSWORD=INSERT_YOUR_POSTGRES_PASSWORD_HERE

# JWT Configuration
JWT_SECRET=INSERT_YOUR_SECURE_JWT_SECRET_HERE_MINIMUM_32_CHARACTERS

# Email Configuration
EMAIL_USER=INSERT_YOUR_EMAIL_USER_HERE
EMAIL_PASSWORD=INSERT_YOUR_EMAIL_PASSWORD_HERE
```

### 2. Docker Commands
```bash
# Start containers
docker-compose up -d

# Stop containers
docker-compose down

# Reset Database
docker-compose down -v
```

### 3. Database Migrations
```powershell
# Create new migration
dotnet ef migrations add MigrationName --project Data --startup-project CoreAPI

# List migrations
dotnet ef migrations list --project Data --startup-project CoreAPI

# Remove last migration
dotnet ef migrations remove --project Data --startup-project CoreAPI

# Generate SQL script
dotnet ef migrations script --project Data --startup-project CoreAPI
```

## Project Structure

```
CoreAPI/
├── Business/                             # Business logic layer
│   ├── DataRepositories/                 # Repository interfaces
│   ├── Extensions/                       # Extensions methods
│   ├── Jobs/                             # Background jobs
│   │   ├── Background/                   # Instant jobs
│   │   └── Scheduled/                    # Scheduled jobs
│   ├── Models/                           # Business models
│   ├── Services/                         # Business services
│   │   └── Base/                         # Interface services
│   └── Business.csproj
│
├── CoreAPI/                              # Main API project
│   ├── AutoMapper/                       # Automapper configuration
│   ├── Controllers/                      # API Controllers
│   ├── Extensions/                       # Extensions and configurations
│   ├── Logging/                          # Logging configurations
│   ├── appsettings.json                  # Application settings
│   │   └── appsettings.Development.json  # Development settings
│   └── Program.cs                        # Application setup
│
├── Data/                                 # Data access layer
│   ├── Context/                          # EF Core context
│   ├── Extensions/                       # EF Core extensions
│   ├── Migrations/                       # Database migrations
│   ├── Models/                           # Data entities
│   └── Repositories/                     # Data repositories
│
├── .env                                  # Environment variables
├── .env.example                          # Environment variables template
├── docker-compose.yml                    # Docker configuration
├── Dockerfile                            # Container build configuration
├── CoreAPI.sln                           # Solution file
├── LICENSE                               # License
└── README.md                             # Project documentation
```

## API Documentation

### Swagger UI

Interactive API documentation and testing interface.

#### Container Management

- **Container Restart:**
  - The Swagger UI will continue to work even after container restarts

- **Volume Management:**
  - If you remove volumes (`docker-compose down -v`), you'll need to refresh the Swagger UI
  - In this case, clear your browser cache or do a hard refresh (Ctrl+F5)

#### Authentication

1. **Get JWT Token:**
   - Use the `/api/auth/login` endpoint
   - Provide username and password
   - Copy the returned token

2. **Configure Swagger Authorization:**
   - Click the "Authorize" button at the top
     - Or the lock at the right side of any endpoint to enable the authentication just for that endpoint
   - In the "Value" field, enter: `Bearer your-token-here`
   - Click "Authorize"
   - All subsequent requests will include the token

3. **Token Format:**
```
Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Testing Endpoints

1. **Select an Endpoint:**
   - Endpoints are grouped by controller
   - Click to expand and see available methods

2. **Make a Request:**
   - Click "Try it out"
   - Fill in required parameters
   - Click "Execute"

3. **View Results:**
   - Response status
   - Response headers
   - Response body
   - Curl command

#### Common Status Codes
- `200`: Success
- `201`: Created
- `400`: Bad Request
- `401`: Unauthorized
- `403`: Forbidden
- `404`: Not Found
- `429`: Too Many Requests
- `500`: Internal Server Error

## Available Services

### 1. Redis Cache

Distributed caching system for performance optimization.

#### Configuration
```csharp
// In Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "CoreAPI_";
});
```

#### Usage

1. **Dependency Injection:**
```csharp
private readonly ICacheService _cacheService;

public MyService(ICacheService cacheService)
{
    _cacheService = cacheService;
}
```

2. **Basic Operations:**
```csharp
// Store in cache
await _cacheService.SetAsync("key", object, TimeSpan.FromMinutes(30));

// Retrieve from cache
var result = await _cacheService.GetAsync<ObjectType>("key");

// Remove from cache
await _cacheService.RemoveAsync("key");

// Check existence
bool exists = await _cacheService.ExistsAsync("key");
```

3. **Cache with Fallback:**
```csharp
var data = await _cacheService.GetOrCreateAsync(
    "key",
    TimeSpan.FromHours(1),
    async () => await _repository.GetDataAsync()
);
```

### 2. Redis Message Broker

Message broker system for asynchronous communication between components.

#### Configuration
```csharp
// In Program.cs
builder.Services.AddMessageBroker(builder.Configuration);
```

#### Usage

1. **Publishing Messages:**
```csharp
private readonly IMessageBrokerService _messageBroker;

// Publish message
await _messageBroker.PublishAsync("channel", new MessageDto 
{
    Data = "example"
});

// Publish with delay
await _messageBroker.PublishWithDelayAsync("channel", message, TimeSpan.FromMinutes(5));
```

2. **Consuming Messages:**
```csharp
// Subscribe to channel
await _messageBroker.SubscribeAsync<MessageDto>("channel", async (message) => 
{
    // Process message
    await ProcessMessage(message);
});

// Subscribe with filter
await _messageBroker.SubscribeAsync<MessageDto>(
    "channel",
    async (msg) => await ProcessMessage(msg),
    msg => msg.Type == "specific"
);
```

### 3. IP Rate Limiting

Request rate limiting system using AspNetCoreRateLimit.

#### Configuration
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1s",
        "Limit": 10
      },
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

#### Usage in Controllers

- **Default**: Follows rules configured in `appsettings.json`

```csharp
[EnableRateLimiting("fixed")]  // Default policy
public class UserController : ControllerBase
{
    [EnableRateLimiting("custom")] // Specific policy
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        // ...
    }
}
```

### 4. Background Jobs (Hangfire)

Background processing system supporting instant and scheduled jobs.

#### Configuration
```csharp
// In Program.cs
builder.Services.AddHangfire(config =>
{
    config.UseRedisStorage(builder.Configuration.GetConnectionString("Redis"));
});
```

#### Usage

1. **Instant Jobs:**
```csharp
private readonly IBackgroundJobService _jobService;

// Enqueue job
var jobId = await _jobService.EnqueueAsync<IEmailService>(
    service => service.SendEmailAsync(email, "subject", "message")
);

// Enqueue job with continuation
await _jobService.EnqueueWithContinuationAsync<IEmailService, INotificationService>(
    emailJob => emailJob.SendEmailAsync(email),
    notifyJob => notifyJob.NotifyDeliveryAsync(email)
);
```

2. **Recurring Jobs:**
```csharp
// Schedule daily job
await _jobService.ScheduleRecurringAsync<IUserService>(
    "users-cleanup",
    service => service.CleanInactiveUsers(),
    "0 0 * * *"  // Every day at midnight
);

// Schedule job every 5 minutes
await _jobService.ScheduleRecurringAsync<IHealthCheckService>(
    "health-check",
    service => service.CheckSystem(),
    "*/5 * * * *"
);