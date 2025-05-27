# CoreAPI - AI-powered Analytics & Consulting Platform API

**CoreAPI** is the backend Web API for an AI-driven analytics and consulting platform tailored for small and medium businesses (SMBs). It provides user management, secure authentication (including 2FA), product and category management, real-time inventory control, and health monitoring.

## Table of Contents
1. [Features](#features)
2. [Prerequisites](#prerequisites)
3. [Setup & Configuration](#setup--configuration)
4. [Project Structure](#project-structure)
5. [API Endpoints](#api-endpoints)
   - [Authentication & User Management](#authentication--user-management-user)
   - [Category Management](#category-management-category)
   - [Product Management](#product-management-product)
   - [Inventory (Stock) Management](#inventory-stock-management-stock)
   - [Health & Monitoring](#health--monitoring-healthcheck)
6. [Swagger & API Documentation](#swagger--api-documentation)
7. [Contributing](#contributing)

## Features
- User registration, login, JWT authentication, and optional 2FA
- Email confirmation, password reset, and token refresh
- Role-based and group-based access control for linked users
- Accent-insensitive search for categories and products with in-memory caching
- Full CRUD for categories and products with validation
- Real-time stock level queries and updates, including low-stock alerts
- Health check endpoints for API status and database connectivity
- Docker, PostgreSQL, Redis, and Hangfire integrations for scalable infrastructure

## Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- PostgreSQL (containerized via Docker Compose)
- Redis (for caching and background jobs)
- (Optional) Visual Studio 2022 or VS Code

## Setup & Configuration
1. Copy `.env.example` to `.env` and fill in your credentials and connection strings.
2. Launch supporting services:
   ```bash
   docker-compose up --build
   ```
3. Apply database migrations:
   ```bash
   dotnet ef database update --project Data --startup-project CoreAPI
   ```
4. Run the API service:
   ```bash
   dotnet run --project CoreAPI
   ```
5. Browse Swagger UI at `http://localhost:5000/swagger`.

## Project Structure
```
CoreAPI/                             # Solution root
├── Business/                        # Business logic and models
├── CoreAPI/                         # Main Web API project
│   ├── Controllers/                 # API controllers
│   ├── Extensions/                  # DI and configuration setups
│   └── Program.cs                   # Entry point
├── Data/                            # EF Core context, entities, and migrations
├── .env                             # Environment variables
├── docker-compose.yml               # Service definitions (Postgres, Redis, Hangfire)
├── Dockerfile                       # API container setup
└── README.md                        # Project documentation
```

## API Endpoints

### Authentication & User Management (`/User`)
- **POST /User/Register**  
  Registers a new user with a Free subscription plan; validates input, assigns roles, and sends confirmation email.
- **GET /User/ConfirmEmail**  
  Confirms a user's email address via token.
- **POST /User/ResendEmailConfirmation**  
  Sends a new email confirmation link.
- **POST /User/Login**  
  Authenticates a user; returns 2FA requirement if enabled.
- **POST /User/ForgotPassword**  
  Initiates password reset by sending a reset token.
- **POST /User/ResetPassword**  
  Resets password using the provided token.
- **POST /User/RefreshToken**  
  Issues new JWT and refresh tokens.
- **GET /User/Setup2FA**  
  Generates a QR code and secret key for two-factor authentication.
- **POST /User/Enable2FA**  
  Verifies 2FA code and enables two-factor authentication.
- **POST /User/Verify2FA**  
  Validates 2FA during login and issues tokens.
- **GET /User**  
  Retrieves the authenticated user's information.

### Category Management (`/Category`)
- **GET /Category/Search**  
  Accent-insensitive search for categories with pagination and per-user caching.
- **GET /Category/{id}**  
  Retrieves details for a specific category within the user's group.
- **POST /Category**  
  Creates a new category under the authenticated user's group.
- **PUT /Category**  
  Updates an existing category's name, description, and active status.
- **DELETE /Category/{id}**  
  Deletes a category if no associated products exist.

### Product Management (`/Product`)
- **GET /Product/Search**  
  Accent-insensitive search for products with pagination and caching.
- **GET /Product/{id}**  
  Retrieves product details, limited to the user's group.
- **POST /Product**  
  Creates a new product; checks for duplicate barcode or SKU.
- **PUT /Product**  
  Updates an existing product's details.
- **DELETE /Product/{id}**  
  Deletes a product by ID.

### Inventory (Stock) Management (`/Stock`)
- **GET /Stock/{productId}**  
  Retrieves the current stock level for a product.
- **PUT /Stock/{productId}**  
  Sets the stock level to a specified quantity.
- **POST /Stock/Add/{productId}**  
  Increases stock by a specified amount, creating a record if none exists.
- **POST /Stock/Deduct/{productId}**  
  Decreases stock by a specified amount; errors on insufficient stock.
- **GET /Stock/Low?threshold={n}**  
  Lists products with stock at or below the given threshold (default: 10).
- **GET /Stock/Check/{productId}?quantity={n}**  
  Checks if sufficient stock is available for a requested quantity.

### Health & Monitoring (`/HealthCheck`)
- **GET /HealthCheck**  
  Returns basic API health status and timestamp.
- **GET /HealthCheck/Database**  
  Verifies database connectivity.
- **GET /HealthCheck/User**  
  Returns information about the current authenticated user.

## Swagger & API Documentation
Access the interactive Swagger UI at `http://localhost:5000/swagger` to explore endpoints. Click **Authorize** and enter `Bearer <token>` to authenticate.

## Contributing
Contributions are welcome! Please open issues or pull requests. Ensure any new endpoints include XML comments and relevant tests.
