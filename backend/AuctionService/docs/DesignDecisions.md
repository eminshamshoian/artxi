# Design Decisions

This document captures the reasoning behind notable implementation choices in the Auction Service.

## One-auction-per-item invariant
- **Decision:** Model the relationship as `Auction (FK: ItemId) -> Item (1:1)` with a unique index on `ItemId`.
- **Why:** Marketplace rules allow only one live auction per item. Enforcing this in the schema prevents race conditions and simplifies controller checks.

## Manual update pipelines
- **Decision:** Do not AutoMap update DTOs directly onto entities; controllers mutate fields explicitly.
- **Why:** Business rules around status transitions, pricing mutability, and publication flow are easier to reason about when applied imperatively. It also guards against unintended property writes when DTOs evolve.

## AutoMapper on create/read paths
- **Decision:** Use a single `MappingProfiles` class to translate between entities and DTOs for create/read scenarios.
- **Why:** Keeps controller code concise while maintaining symmetry between stored data and API responses. Ignoring server-controlled fields ensures invariants remain intact.

## Persistence provider strategy
- **Decision:** Target PostgreSQL for production, while enabling SQLite for tests via conditional value converters.
- **Why:** PostgreSQL offers robust JSON and enum support for production workloads. SQLite provides fast, in-memory execution for tests but lacks native `DateTimeOffset`, list, and dictionary support—custom converters bridge the gap so tests mirror production semantics.

## Seed data in development
- **Decision:** Run `DbInitializer.InitDb` at startup with a toggle for seeding.
- **Why:** Having auctions in multiple lifecycle stages accelerates manual QA and UI prototyping. The seeding code is guarded so production environments can disable it.

## Targeting .NET 10 (preview)
- **Decision:** Adopt the latest SDK to access modern language features and platform updates.
- **Why:** Aligns with the broader solution roadmap. CI/CD pipelines must pin to the preview SDK until the LTS release lands.

## Testing approach
- **Decision:** Use integration-style controller tests with a shared SQLite DB per class and FluentAssertions for expressive expectations.
- **Why:** Validates controller behaviour, EF Core configuration, and AutoMapper profiles in one pass. This ensures domain rules are enforced end-to-end.

## Logging abstraction usage
- **Decision:** Register `NullLoggerFactory` when constructing test-time AutoMapper configurations.
- **Why:** The latest AutoMapper constructors expect a logger factory. Tests do not require logging, so the null implementation keeps the setup simple while satisfying dependencies.
