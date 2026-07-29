# HealthVault

HealthVault is an ASP.NET Core backend scaffold based on the structure of the
FinanceTracker API.

## Technology

- .NET 6
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Swagger / OpenAPI
- Newtonsoft.Json

## Project structure

```text
HealthVault.sln
HealthVaultAPI/
  HealthVaultAPI/
    Controllers/
    Models/
    Properties/
    Program.cs
    appsettings.json
    HealthVaultAPI.csproj
```

The project currently contains infrastructure only. Domain models, API
operations, database migrations, authentication, and business logic have not
been implemented.

## Configuration

Set the `ConnectionStrings:HealthVaultCon` value through local configuration,
user secrets, or environment variables before using the database.

Do not commit credentials or production connection strings.

## Run locally

```powershell
dotnet restore HealthVault.sln
dotnet run --project HealthVaultAPI/HealthVaultAPI/HealthVaultAPI.csproj
```

Swagger is available at `/swagger` while running in the Development
environment.
