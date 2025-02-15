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
   - Make sure to set a strong `JWT_SECRET` in your `.env` file for token signing

## Authentication

The API uses JWT (JSON Web Token) for authentication. Here's how to use it:

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

2. **Login to get a token:**
   ```http
   POST /User/Login
   Content-Type: application/json

   {
     "email": "your_email@example.com",
     "password": "your_password"
   }
   ```
   The response will include your JWT token.

3. **Using the token:**
   - Add the token to the Authorization header of your requests:
   ```
   Authorization: Bearer your_token_here
   ```
   - In Swagger UI:
     1. Click the "Authorize" button at the top of the page
     2. In the popup, enter your token in the format: `Bearer your_token_here`
     3. Click "Authorize" to save
     4. Now all your requests in Swagger will include the token

   - Protected endpoints (with [Authorize] attribute) require this token

4. **JWT Configuration:**
   - The token issuer and audience are configured in `appsettings.json`
   - The signing key (`JWT_SECRET`) must be set in your `.env` file
   - Tokens expire after 3 hours

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
├── AutoMapper/                # Automapper configuration
│   └── AutoMapperProfile.cs   # Mapping configuration
├── Controllers/               # API Controllers
│   ├── HealthCheckController  # Health check endpoints
│   └── UserController         # Authentication endpoints
├── Models/                    # API Models
│   ├── LoginModel             # Login request model
│   └── UserModel              # User registration model
├── Data/                      # Data access layer
│   ├── Context/               # Database context
│   │   └── CoreAPIContext     # EF Core DB context
│   ├── Migrations/            # Database migrations
│   ├── Models/                # Database models
│   └── Repositories/          # Data repositories 
├── Business/                  # Business layer
│   ├── DataRepositories/      # Database repositories interfaces
│   ├── Models/                # Business models
│   └── Services/              # Business services
│       └── Base/              # Service interfaces
├── Properties/                # Project settings
├── CoreAPI.csproj             # .NET project file
├── Program.cs                 # Application entry point and DI configuration
├── appsettings.json           # Application settings (including JWT config)
├── appsettings.*.json         # Environment-specific settings
├── Dockerfile                 # Container build instructions
├── docker-compose.yml         # Docker Compose configuration
├── .env                       # Environment variables (not in repo)
└── .env.example               # Example environment variables
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
dotnet ef migrations add AddNewFeature --project Data --startup-project CoreAPI

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
