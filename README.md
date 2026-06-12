# NTierArchitecture

This repository is a base ASP.NET Core project for starting future backend APIs. It is not intended to be a finished business product. The goal is to provide a ready-to-extend NTier/Clean Architecture style foundation with authentication, persistence, caching, common middleware, and basic infrastructure wiring already in place.

## What This Base Includes

- NTier project split: API, Application, Domain, and Infrastructure.
- JWT authentication with access and refresh token sessions stored in Redis.
- HttpOnly authentication cookies for login sessions.
- Role-based authorization policies
- EF Core with PostgreSQL.
- BCrypt password hashing.
- SignalR notification hub foundation.
- Basic performance logging, rate limiting, and global exception middleware.

## Project Structure

```text
NTierArchitecture/
  NTierArchitecture.API/
    Controllers/          HTTP endpoints
    Extensions/           API-specific helpers such as auth cookies
    Identity/             Custom authorization attributes/constants
    Middlewares/          Exception, performance, and rate-limit middleware
    Services/             API-level services such as current-claim access
    Validation/           Fluent Validation
    Program.cs            Application startup and request pipeline

  NTierArchitecture.Application/
    Abstractions/         Shared result and third-party response models
    Commons/              Pagination, queue, and common helper models
    DTOs/                 Request and response DTOs
    IRepositories/        Repository contracts
    IServices/            Service contracts
    Mappers/              AutoMapper profiles
    Services/             Business/application services
    Settings/             Strongly typed settings models
    Utils/                Utility helpers

  NTierArchitecture.Domain/
    Entities/             Core domain entities
    Enums/                Domain enums

  NTierArchitecture.Infrastructure/
    Database/             EF Core DbContext
    Hubs/                 SignalR hub implementation
    Repositories/         EF Core repository implementations
    Migrations/           EF Core migrations
    UnitOfWork.cs         Unit of Work implementation
```

## Architecture Direction

The intended dependency direction is:

```text
API -> Application -> Domain
API -> Infrastructure -> Application -> Domain
```

- `Domain` contains core entities and enums. It should stay framework-light.
- `Application` contains DTOs, interfaces, business services, mapping, and use-case logic.
- `Infrastructure` implements persistence, Redis, Cloudinary, SignalR, and other external integrations.
- `API` wires dependency injection, middleware, authentication, Swagger, and controllers.

When adding features, prefer putting business rules in `Application`, storage details in `Infrastructure`, and HTTP-only concerns in `API`.

## Prerequisites

- .NET 8 SDK
- Docker Desktop or another Docker-compatible runtime
- PostgreSQL and Redis, either from Docker Compose or local installs

```bash
dotnet tool install --global dotnet-ef
```

## Configuration

The API reads configuration from `NTierArchitecture.API/appsettings.json`, environment-specific appsettings files, user secrets, and environment variables.

Required settings:

- `ConnectionStrings:DefaultConnection`
- `ConnectionStrings:Redis`
- `JwtSettings:SecretKey`
- `JwtSettings:Issuer`
- `JwtSettings:Audience`
- `CloudinarySetting:CloudName`
- `CloudinarySetting:ApiKey`
- `CloudinarySetting:ApiSecret`
- `CloudinarySetting:Folder`

Do not commit real production secrets.

## Run Locally

Start PostgreSQL and Redis:

```bash
docker compose up -d
```

Restore and build:

```bash
dotnet restore
dotnet build
```

Apply EF Core migrations:

```bash
dotnet ef database update --project NTierArchitecture.Infrastructure --startup-project NTierArchitecture.API
```

Run the API:

```bash
dotnet run --project NTierArchitecture.API --launch-profile https
```

Default local URLs from `launchSettings.json`:

- HTTPS API: `https://localhost:7294`
- Swagger UI: `https://localhost:7294/swagger`
- Health check: `https://localhost:7294/healthchecks`

## Middleware And Cross-Cutting Pieces

The API pipeline includes:

- Swagger and Swagger UI in development.
- CORS policy registration.
- Performance logging for `/api` requests.
- JWT authentication and authorization.
- Global exception middleware.
- Basic in-memory rate limiting.
- HTTPS redirection.
- Health checks.
- Controller routing.
- SignalR notification hub at `/hub/notificationHub`.

## Useful Commands

Create a migration:

```bash
dotnet ef migrations add <MigrationName> --project NTierArchitecture.Infrastructure --startup-project NTierArchitecture.API
```

Update the database:

```bash
dotnet ef database update --project NTierArchitecture.Infrastructure --startup-project NTierArchitecture.API
```

Build the solution:

```bash
dotnet build
```

Run the API:

```bash
dotnet run --project NTierArchitecture.API --launch-profile https
```

Stop local infrastructure:

```bash
docker compose down
```

Stop local infrastructure and remove volumes:

```bash
docker compose down -v
```
