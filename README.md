# CoreAPI - .NET Web API

A Web API project using .NET 8.0 and Docker with PostgreSQL database.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) (optional)
- [VS Code](https://code.visualstudio.com/) (optional)

## Initial Setup

1. **Dependencies:**
   ```powershell
   # Restore .NET packages
   dotnet restore

   # Install Entity Framework Core tools (required for migration commands)
   dotnet tool install --global dotnet-ef
   ```

2. **Environment Configuration:**
   - Create a `.env` file in the root directory with the variables from `.env.example`
   - Set a strong `JWT_SECRET` for token signing
   - Configure email settings:
     - `EMAIL_USER`: Your Gmail address
     - `EMAIL_PASSWORD`: App password from Google (requires 2FA)

## Authentication & Security Features

### User Registration and Email Verification

1. **Register a new user:**
   ```http
   POST /User/Register
   Content-Type: application/json

   {
     "username": "your_username",
     "email": "your_email@example.com",
     "password": "your_password"
   }
   ```
   - A confirmation email will be sent
   - Password must be at least 8 characters with uppercase, lowercase, number, and special character

2. **Confirm Email:**
   ```http
   POST /User/ConfirmEmail
   Content-Type: application/json

   {
     "email": "your_email@example.com",
     "token": "token-from-email"
   }
   ```

3. **Resend Confirmation Email:**
   ```http
   POST /User/ResendEmailConfirmation
   Content-Type: application/json

   {
     "email": "your_email@example.com"
   }
   ```

### Password Reset

1. **Request Password Reset:**
   ```http
   POST /User/ForgotPassword
   Content-Type: application/json

   {
     "email": "your_email@example.com"
   }
   ```
   - Reset token will be sent via email

2. **Reset Password:**
   ```http
   POST /User/ResetPassword
   Content-Type: application/json

   {
     "email": "your_email@example.com",
     "token": "token-from-email",
     "newPassword": "your-new-password"
   }
   ```

### JWT Authentication

1. **Login to get tokens:**
   ```http
   POST /User/Login
   Content-Type: application/json

   {
     "email": "your_email@example.com",
     "password": "your_password"
   }
   ```
   Response includes:
   - `accessToken`: For API authentication
   - `refreshToken`: To get new access tokens

2. **Refresh Token:**
   ```http
   POST /User/RefreshToken
   Content-Type: application/json

   {
     "accessToken": "expired-access-token",
     "refreshToken": "your-refresh-token"
   }
   ```

3. **Using Access Tokens:**
- In Swagger UI:
     1. Click the "Authorize" button at the top of the page
     2. In the popup, enter your token in the format: `Bearer your_token_here`
     3. Click "Authorize" to save
     4. Now all your requests in Swagger will include the token

   - Protected endpoints (with [Authorize] attribute) require this token

   ```
   Authorization: Bearer your_access_token
   ```

### Two-Factor Authentication (2FA)

The API supports Two-Factor Authentication using Time-based One-Time Password (TOTP). Compatible with authenticator apps like Google Authenticator, Microsoft Authenticator, or Authy.

#### Setting up 2FA

1. Get the QR code and setup key:
```http
GET /api/User/Setup2FA
Authorization: Bearer {your_token}
```

Response:
```json
{
    "qrCodeUrl": "data:image/png;base64,...",
    "manualEntryKey": "YOUR_SECRET_KEY"
}
```

2. Scan the QR code with your authenticator app or manually enter the key.

3. Enable 2FA by verifying the code from your app:
```http
POST /api/User/Enable2FA
Authorization: Bearer {your_token}
Content-Type: application/json

{
    "code": "123456"
}
```

#### Login Flow with 2FA

1. Initial login with email/password:
```http
POST /api/User/Login
Content-Type: application/json

{
    "email": "user@example.com",
    "password": "your_password"
}
```

If 2FA is enabled, you'll receive:
```json
{
    "requiresTwoFactor": true,
    "email": "user@example.com"
}
```

2. Complete login by verifying the 2FA code:
```http
POST /api/User/Verify2FA
Content-Type: application/json

{
    "email": "user@example.com",
    "code": "123456"
}
```

Success response:
```json
{
    "accessToken": "jwt_token",
    "refreshToken": "refresh_token",
    "requiresTwoFactor": false
}
```

#### Important Notes

- The refresh token flow does not require 2FA verification
- Account lockout applies to both password and 2FA code attempts (5 failed attempts = 15 minutes lockout)
- Store the manual entry key securely as a backup
- 2FA codes are time-based and change every 30 seconds

## Message Broker System

The API uses Redis as a message broker for handling asynchronous operations. This allows for better scalability and reliability in processing operations like email sending and background tasks.

### Using the Message Broker

1. **Publishing Messages:**
```csharp
// Inject the service
private readonly IMessageBrokerService _messageBroker;

// Publish a message
await _messageBroker.PublishAsync("channel_name", new MessageData 
{
    Property1 = "value",
    Property2 = 123
});
```

2. **Subscribing to Messages:**
```csharp
// Subscribe to a channel
await _messageBroker.SubscribeAsync<MessageData>("channel_name", async message =>
{
    // Handle the message
    await ProcessMessage(message);
});
```

3. **Example with Email Service:**
```csharp
// Publishing an email
var emailMessage = new EmailMessage
{
    To = "user@example.com",
    Subject = "Welcome",
    Body = "Welcome to our platform!"
};
await _messageBroker.PublishAsync("email_notifications", emailMessage);

// Processing emails (in EmailSenderService)
await _messageBroker.SubscribeAsync<EmailMessage>("email_notifications", async message =>
{
    await SendEmailAsync(message);
});
```

### Benefits
- Asynchronous processing
- Improved scalability
- Better error handling
- Reduced system coupling
- Real-time message delivery
- Persistent message queue

## Security Features

- JWT authentication with refresh tokens
- Email verification for new accounts
- Secure password reset flow
- Rate limiting to prevent abuse
- Password complexity requirements
- Email notifications for security events
- Environment-based configuration
- Docker containerization
- Two-Factor Authentication (2FA)

### Caching and Rate Limiting

The API implements a robust caching and rate limiting system using Redis as a distributed cache. This setup allows for improved performance and better control over API usage across multiple instances.

#### Rate Limiting
- 10 requests per second per IP
- 100 requests per minute per IP
- Returns HTTP 429 when limit exceeded
- Configured in `appsettings.json`
- Uses Redis for distributed rate limiting storage

#### Redis Cache Configuration

Redis is configured via Docker and starts automatically with the application. The configuration can be found in `appsettings.json`:

```json
"Redis": {
  "Configuration": "redis:6379",
  "InstanceName": "CoreAPI_Redis_Cache"
}
```

#### Using the Cache

To use caching in a controller, inject `IDistributedCache`:

```csharp
using Microsoft.Extensions.Caching.Distributed;

public class ExampleController : ControllerBase
{
    private readonly IDistributedCache _cache;

    public ExampleController(IDistributedCache cache)
    {
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // Try to get from cache
        string key = "example_key";
        string? cachedValue = await _cache.GetStringAsync(key);

        if (cachedValue != null)
        {
            return Ok(cachedValue);
        }

        // If not in cache, generate new value
        string newValue = "New value generated at " + DateTime.Now;

        // Save to cache for 10 minutes
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        await _cache.SetStringAsync(key, newValue, options);
        return Ok(newValue);
    }
}
```

#### Available Cache Methods

`IDistributedCache` provides the following main methods:

```csharp
// Strings
await _cache.GetStringAsync(key);
await _cache.SetStringAsync(key, value, options);

// Bytes
await _cache.GetAsync(key);
await _cache.SetAsync(key, byteArray, options);

// Removal
await _cache.RemoveAsync(key);
```

#### Cache Options

Configure cache options using `DistributedCacheEntryOptions`:

```csharp
var options = new DistributedCacheEntryOptions
{
    // Absolute expiration time
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
    
    // OR sliding expiration (renews on each access)
    SlidingExpiration = TimeSpan.FromMinutes(2)
};
```

This distributed caching system improves application performance by reducing database load and API response times, while the distributed rate limiting ensures fair API usage across all instances.

## Running the Application

### Using Docker Compose
```powershell
# Start the application
docker compose up --build

# View logs
docker compose logs -f

# Stop the application
docker compose down
```

The application will be available at:
- Swagger UI: http://localhost:5000/swagger
  - Use this interface to test the API endpoints and view documentation
  - All endpoints are documented with examples and response types
  - Protected endpoints are marked with a lock icon 
- Health Check: http://localhost:5000/healthcheck
  - Basic health check endpoint (no auth required)
- Database Check: http://localhost:5000/healthcheck/database
  - Checks database connectivity (no auth required)
- User Check: http://localhost:5000/healthcheck/user
  - Protected endpoint to verify authentication

### IDE Setup

#### Visual Studio 2022
- Required extensions: Docker
- Open `CoreAPI.sln`
- Use the integrated terminal for Docker commands

#### VS Code
- Required extensions:
  - Docker
  - Dev Containers
  - C# Dev Kit
- Open the project folder
- Use the integrated terminal for Docker commands

## Project Structure

```
CoreAPI/
├── CoreAPI/                           # Main API project
│   ├── AutoMapper/                    # Automapper configuration
│   ├── Controllers/                   # API Controllers
│   ├── Models/                        # API Models
│   ├── Services/                      # Local services
│   ├── appsettings.json               # Main configuration
│   ├── appsettings.Development.json
│   ├── Program.cs                     # Application setup
│   └── CoreAPI.csproj
│
├── Business/                          # Business logic layer
│   ├── DataRepositories/              # Repository interfaces
│   │   └── Base/
│   ├── Models/                        # Business models
│   ├── Services/                      # Business services
│   │   └── Base/                      # Service interfaces
│   └── Business.csproj
│
├── Data/                              # Data access layer
│   ├── Context/                       # Database context
│   │   └── CoreAPIContext.cs
│   ├── Migrations/                    # EF Core migrations
│   ├── Models/                        # Database models
│   ├── Repositories/                  # Repository implementations
│   └── Data.csproj
│
├── docker-compose.yml                 # Docker configuration
├── Dockerfile                         # Container build
├── .env                               # Environment variables (not in repo)
├── .env.example                       # Environment variables template
└── README.md                          # Project documentation
```

## Database Migrations

### Overview
- Migrations are managed by Entity Framework Core
- The application automatically applies pending migrations on startup
- Migration files are stored in `Data/Migrations`

### Commands
1. **Add Migration Files**
   ```powershell
   # Create a new migration
   dotnet ef migrations add MigrationName --project Data --startup-project CoreAPI --verbose
   ```

2. **What This Creates**
   - Two files in `Data/Migrations`:
     - `YYYYMMDDHHMMSS_MigrationName.cs`: Contains Up() and Down() methods
     - `YYYYMMDDHHMMSS_MigrationName.Designer.cs`: Contains migration metadata
   - The migration will be applied automatically when the application starts

### Managing Migrations
```powershell
# If you need to update the tools
dotnet tool update --global dotnet-ef

# Create a new migration after model changes
dotnet ef migrations add MigrationName --project Data --startup-project CoreAPI

# List all migrations and their status
dotnet ef migrations list --project Data --startup-project CoreAPI

# Remove last migration (if not applied)
dotnet ef migrations remove --project Data --startup-project CoreAPI

# Generate SQL script (useful for reviewing changes)
dotnet ef migrations script --project Data --startup-project CoreAPI
```

### Development Workflow
1. Make changes to entity models in `Data/Models`
2. Create a new migration to capture those changes
3. Application will apply migrations automatically on next startup
4. Verify database state using the health check endpoint

## Troubleshooting

### Docker Issues

1. **Port Conflicts**
   ```powershell
   # Check processes using port 5000
   netstat -ano | findstr :5000
   
   # Stop all containers
   docker compose down
   ```

2. **Clean Docker Resources**
   ```powershell
   # Remove unused containers
   docker container prune

   # Remove unused images
   docker image prune -a

   # Remove unused volumes
   docker volume prune
   ```

3. **Full Rebuild**
   ```powershell
   # Remove everything and rebuild
   docker compose down --rmi all -v
   docker compose up --build
   ```

### Package Management

```powershell
# List installed packages
dotnet list package

# Add package
dotnet add package PackageName

# Remove package
dotnet remove package PackageName

# Update package
dotnet add package PackageName --version Version
```

Note: The application will automatically create the database and apply migrations on startup.