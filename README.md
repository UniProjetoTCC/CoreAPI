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

## Security Features

- JWT authentication with refresh tokens
- Email verification for new accounts
- Secure password reset flow
- Rate limiting to prevent abuse
- Password complexity requirements
- Email notifications for security events
- Environment-based configuration
- Docker containerization

### Rate Limiting

The API implements IP-based rate limiting:
- 10 requests per second per IP
- 100 requests per minute per IP
- Returns HTTP 429 when limit exceeded
- Configured in `appsettings.json`

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
   dotnet ef migrations add MigrationName --project Data --startup-project CoreAPI
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