# Architecture

The Auction Service follows a layered architecture that separates HTTP presentation, domain modelling, and persistence concerns while remaining lightweight.

```
┌──────────────────────┐
│ ASP.NET Core Host    │  Program.cs, Controllers, DTOs
├──────────────────────┤
│ Application Layer    │  (placeholder today, reserved for future services)
├──────────────────────┤
│ Domain Layer         │  Entities, Enums, Business Invariants
├──────────────────────┤
│ Infrastructure Layer │  EF Core DbContext, migrations, seeding
└──────────────────────┘
```

## Runtime topology

- **Host:** ASP.NET Core Web API running on Kestrel.
- **Persistence:** PostgreSQL in production (via `UseNpgsql`). Unit tests swap in SQLite through the same EF Core context.
- **Object mapping:** AutoMapper with a single profile (`MappingProfiles`) registered at startup.
- **Documentation:** Swagger/OpenAPI enabled when `IHostEnvironment` is development.

## Data access

`AuctionDbContext` is the single gateway to the database. It configures:
- 1:1 relationship between `Auction` and `Item` with restrictive deletes.
- Decimal precision mapping for all monetary amounts.
- Enum-to-string conversion so database values remain human-readable and evolve safely.
- Conditional value converters when using SQLite (serialises list/dictionary metadata to JSON and stores `DateTimeOffset` values as ticks).

`DbInitializer` is invoked from `Program.cs` to ensure migrations are applied and, optionally, to seed a representative dataset.

## API surface vs domain

Controllers operate close to the domain model, applying validation logic before persisting changes. AutoMapper handles DTO ↔ entity conversion for create/read paths, while update flows manually manipulate the entity to respect business rules.

The following boundaries are enforced:

- Controllers work with DTOs and orchestrate security/validation.
- Domain entities remain persistence-friendly POCOs.
- Infrastructure owns EF Core specifics and is the only layer that references the database provider packages.

## External dependencies

| Dependency | Usage |
| --- | --- |
| `Microsoft.EntityFrameworkCore.*` | ORM, migrations, provider-specific functionality. |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Production database provider. |
| `Microsoft.EntityFrameworkCore.Sqlite` | Test-only provider used with in-memory connections. |
| `AutoMapper` | DTO ↔ entity transformations. |
| `FluentAssertions`, `xUnit` | Testing stack for behavioural assertions. |

## Build & deployment considerations

- The solution targets .NET 10 (preview). CI environments must install the matching SDK.
- Database migrations live in the infrastructure project; apply them before deploying.
- Any hosted deployment should supply a `DefaultConnection` string pointing to PostgreSQL.
- Seed data runs automatically when `seedSampleData` is true (current implementation toggles based on environment); disable it in production.

## Growth trajectory

The solution intentionally keeps the `Application` project empty to provide a space for domain services or CQRS handlers as complexity grows (e.g., bidding workflows, event publishing). Similarly, infrastructure can host repositories, caching, or messaging integrations as needed, provided controllers remain thin and testable.
