# HealthVault

HealthVault is an ASP.NET Core backend scaffold based on the structure of the
FinanceTracker API.

## Technology

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Swagger / OpenAPI
- Newtonsoft.Json

## Project structure

```text
src/
  HealthVault.sln
  HealthVault.Domain/
    HealthVault.Domain.csproj
  HealthVault.Application/
    Common/
    People/
    Requests/
    HealthVault.Application.csproj
  HealthVault.Persistence/
    Data/
      AppDbContext.cs
    HealthVault.Persistence.csproj
  HealthVault.Web/
    client/
    Controllers/
    Properties/
    Program.cs
    appsettings.json
    HealthVault.Web.csproj
```

API operations use thin controllers that send commands and queries through
MediatR. Their handlers and FluentValidation validators live in the Application
project.

## Configuration

Set the `ConnectionStrings:HealthVaultCon` value through local configuration,
user secrets, or environment variables before using the database.

Do not commit credentials or production connection strings.

## Run locally

```powershell
dotnet restore src/HealthVault.sln
dotnet run --project src/HealthVault.Web/HealthVault.Web.csproj
```

Swagger is available at `/swagger` while running in the Development
environment.
