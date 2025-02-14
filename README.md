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
- Health Check: http://localhost:5000/healthcheck
- Database Check: http://localhost:5000/healthcheck/database

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
├── Controllers/          # API Controllers
├── Data/                # Data access layer
│   ├── Context/        # Database context
│   ├── Models/         # Data models
│   ├── Migrations/     # Database migrations
│   └── Repository/     # Data repositories
├── Business/           # Business logic layer
│   ├── Models/        # Business models
│   ├── Services/      # Business services
│   └── DataRepository/# Data access interfaces
├── Properties/         # Project settings
├── CoreAPI.csproj     # .NET project file
├── Program.cs         # Application entry point
├── Dockerfile         # Container build instructions
├── docker-compose.yml # Docker Compose configuration
├── .env              # Environment variables
└── appsettings.json  # Application settings
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
