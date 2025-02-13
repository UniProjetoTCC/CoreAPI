# CoreAPI - .NET Web API

A Web API project using .NET 8.0 and Docker. This guide explains how to run the project using Docker Compose in Visual Studio and VS Code.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) (optional)
- [VS Code](https://code.visualstudio.com/) (optional)

## Running in Visual Studio 2022

1. **Open the Project:**
   - Open the `CoreAPI.sln` file
   - Or use "File > Open > Project/Solution"

2. **Using the Integrated Terminal:**
   - Open terminal in Visual Studio (View > Terminal or Ctrl+`)
   - Navigate to the project folder (if needed)
   - Run the command:
     ```powershell
     docker compose up --build
     ```
   - The project will build and start automatically
   - Access http://localhost:5000 in your browser

3. **To Stop the Containers:**
   - In terminal: press Ctrl+C or run:
     ```powershell
     docker compose down
     ```

## Running in VS Code

1. **Required Extensions:**
   - Docker
   - Dev Containers

2. **Open the Project:**
   - File > Open Folder
   - Select the project folder

3. **Run with Docker Compose:**
   - Open terminal in VS Code (View > Terminal or Ctrl+`)
   - Run the command:
     ```powershell
     docker compose up --build
     ```
   - The project will build and start automatically
   - Access http://localhost:5000 in your browser

4. **Monitor Containers:**
   ```powershell
   # View real-time logs
   docker compose logs -f

   # List running containers
   docker compose ps
   ```

5. **Stop the Containers:**
   ```powershell
   # Stop and remove containers
   docker compose down
   ```

## Project Structure

```
CoreAPI/
├── Controllers/           # API Controllers
├── Properties/           # Project settings
├── CoreAPI.csproj       # .NET project file
├── Program.cs           # Application entry point
├── Dockerfile           # Container build instructions
├── docker-compose.yml   # Docker Compose configuration
└── appsettings.json     # Application settings
```

## Project URLs

- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger

## Docker Troubleshooting

1. **Port in use error:**
   ```powershell
   # Check processes using the port
   netstat -ano | findstr :5000
   
   # Stop all containers
   docker compose down
   ```

2. **Clean Docker resources:**
   ```powershell
   # Remove stopped containers
   docker container prune

   # Remove unused images
   docker image prune -a

   # Remove unused volumes
   docker volume prune
   ```

3. **Full rebuild:**
   ```powershell
   # Stop containers, remove images and volumes
   docker compose down --rmi all -v

   # Rebuild and start
   docker compose up --build
   ```

## Package Management

### Visual Studio
1. Solution Explorer > Right-click project
2. Manage NuGet Packages
3. Browse to add new ones
4. Installed to view/remove existing
5. Updates to update

### VS Code/Terminal
```powershell
# List installed packages
dotnet list package

# Add package
dotnet add package PACKAGE_NAME

# Remove package
dotnet remove package PACKAGE_NAME

# Restore packages
dotnet restore
```

### Environment-Specific Packages
In the `CoreAPI.csproj` file:

```xml
<!-- Packages for all environments -->
<ItemGroup>
    <PackageReference Include="Common.Package" Version="1.0.0" />
</ItemGroup>

<!-- Development-only packages -->
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
    <PackageReference Include="Dev.Package" Version="1.0.0" />
</ItemGroup>

<!-- Production-only packages -->
<ItemGroup Condition="'$(Configuration)' == 'Release'">
    <PackageReference Include="Prod.Package" Version="1.0.0" />
</ItemGroup>
