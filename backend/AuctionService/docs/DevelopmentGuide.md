# Development Guide

Practical notes for working on the Auction Service locally.

## Prerequisites
- .NET 10.0 RC SDK (`10.0.100-rc.1.25451.107` as referenced in the solution).
- PostgreSQL instance for local development (tests use SQLite in-memory automatically).
- Access to the solution root: `backend/AuctionService`.

## Project structure
```
backend/AuctionService/
├── AuctionService.sln
├── src/
│   ├── AuctionService.Api/            # ASP.NET Core host + controllers
│   ├── AuctionService.Domain/         # Entities, enums
│   ├── AuctionService.Infrastructure/ # DbContext, migrations, seeding
│   └── AuctionService.Application/    # Reserved for future domain services
└── tests/
    └── AuctionService.Tests/          # Integration-style controller tests
```

## Running the API
1. Ensure `appsettings.Development.json` (or user secrets) contains a `DefaultConnection` pointing to PostgreSQL.
2. From `backend/AuctionService`, run:
   ```bash
   dotnet run --project src/AuctionService.Api
   ```
3. Swagger UI becomes available at `https://localhost:<port>/swagger` in development.
4. On first launch the service will migrate the database and seed sample data if running in Development mode.

## Applying migrations
- Add a migration:
  ```bash
  dotnet ef migrations add <MigrationName> --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api
  ```
- Apply migrations (usually handled automatically by `DbInitializer`):
  ```bash
  dotnet ef database update --project src/AuctionService.Infrastructure --startup-project src/AuctionService.Api
  ```

## Running tests
```
dotnet test
```
The test project uses an in-memory SQLite database with schema creation per test scope. Custom value converters in `AuctionDbContext` keep behaviour consistent with PostgreSQL by serialising collections and `DateTimeOffset` values.

## Debugging tips
- **AutoMapper configuration issues**: Call `MapperFactory.CreateMapper()` locally—`AssertConfigurationIsValid()` will throw with detailed mapping hints.
- **SQLite-specific errors**: Ensure new list/dictionary or `DateTimeOffset` properties have conversions inside the `Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"` block.
- **Status transitions**: Update tests in `AuctionsControllerTests` whenever you add new allowed transitions to prevent regressions.

## Extending the domain
- Add new fields to entities together with DTO updates, AutoMapper profile adjustments, and migrations.
- Keep controller logic focused on orchestration; extract reusable business logic into the (currently sparse) Application project once it grows.
- Update documentation and tests in tandem so behaviour remains discoverable and verifiable.
