# Auction Service Overview

The Auction Service exposes HTTP APIs for managing digital art auctions and their underlying catalog items. It is built as an ASP.NET Core Web API that persists state through Entity Framework Core and maps between domain entities and transport DTOs via AutoMapper.

## Responsibilities at a glance

- Maintain a catalog of `Item` records that describe digital collectibles, including metadata, media pointers, and publication status.
- Orchestrate one-to-one auctions per item, enforcing invariants such as "one auction per item" and immutable pricing once an auction has started.
- Surface read models that power marketplace views (full auction detail, lightweight list rows, item listings).
- Apply guardrails around status transitions and update flows so that controller actions remain consistent with marketplace rules.

## Key runtime components

| Component | Location | Purpose |
| --- | --- | --- |
| ASP.NET Core host | `src/AuctionService.Api` | Entry point (`Program.cs`), controller endpoints, AutoMapper profiles, DTO definitions. |
| Domain model | `src/AuctionService.Domain` | Strongly-typed entities (`Auction`, `Item`) and supporting enums for status, media, and licensing. |
| Persistence | `src/AuctionService.Infrastructure` | `AuctionDbContext`, EF Core model configuration, PostgreSQL migrations, and SQLite-friendly conversions used by tests. |
| Database bootstrap | `DbInitializer` | Applies migrations on startup and seeds illustrative auctions/items during development. |
| Tests | `tests/AuctionService.Tests` | Integration-style controller tests that exercise CRUD flows using an in-memory SQLite database. |

## Data model

### Item

Items represent on-chain or off-chain artwork metadata. Notable fields:

- `Title`, `Description`, optional creator attribution, collection metadata.
- Media metadata (`MediaType`, `MimeType`, `AssetUrl`, `PreviewUrl`, `ThumbnailUrl`, optional dimensions and file size).
- Rights and royalties (`License`, `RoyaltyBps`).
- Marketplace status (`ItemStatus`: Draft → Published → Archived).
- Flexible tagging (`List<string> Tags`) and attribute bag (`Dictionary<string,string> Attributes`).
- Publication timestamps (`CreatedAt`, optional `PublishedAt`).
- Optional back-reference to a single `Auction`.

### Auction

Auctions form a strict 1:1 relationship with items via `Auction.ItemId`. Key aspects:

- Seller identity and optional winner details after settlement.
- Pricing inputs: starting price, reserve, buy-now, minimum bid increment, currency.
- State tracking: `AuctionStatus` spans Draft, Scheduled, Live, Ended, ReserveNotMet, Cancelled, Settled.
- Temporal fields (`StartsAt`, `EndsAt`, `CreatedAt`, `UpdatedAt`), with optimistic concurrency handled via `RowVersion`.
- Financial outputs such as `CurrentHighBid` and `SoldAmount`.

EF Core enforces the 1:1 relationship and serialises tags/attributes and `DateTimeOffset` columns appropriately, choosing JSON/text storage strategies for SQLite test runs and richer PostgreSQL types in production.

## HTTP API surface

### `/api/auctions`

- `GET /api/auctions` – Filter by `status`, optionally include item payload, search title/description, and paginate results. Orders by `UpdatedAt` (converted to ticks for SQLite compatibility).
- `GET /api/auctions/{id}` – Retrieve a single auction including the item payload.
- `POST /api/auctions/with-item` – Create a new auction while simultaneously creating a brand-new item record. Validates timeline (end after start) and assigns server-side identifiers.
- `POST /api/auctions` – Attach a new auction to an existing item. Validates end after start, item existence, and that no other auction already references the item.
- `PUT /api/auctions/{id}` – Update scheduling, pricing, and status. Implements domain rules:
  - Reject edits after `EndsAt`.
  - Allow pre-start schedule/pricing adjustments with validation (future `StartsAt`, non-negative prices, positive increments).
  - Freeze pricing/timing after the auction starts.
  - Constrain status transitions to safe combinations (e.g., Scheduled → Cancelled before start).
- `DELETE /api/auctions/{id}` – Remove an auction record (no cascade to item due to restrictive FK).

### `/api/items`

- `GET /api/items` – List items with search and pagination, ordered by `CreatedAt`.
- `GET /api/items/{id}` – Retrieve a single item; returns HTTP 404 if missing.
- `POST /api/items` – Create a draft or published item. Server populates identity and timestamps.
- `PUT /api/items/{id}` – Update mutable display metadata, optionally publish a draft item (enforces Draft-only publishing). Immutable file/media properties are guarded.
- `DELETE /api/items/{id}` – Remove an item only if no auction is linked.

All controllers rely on AutoMapper (`MappingProfiles.cs`) to translate between request DTOs and domain entities, while intentionally avoiding AutoMapper for update DTOs so business rules are enforced manually.

## Request flow example

1. **Create item + auction** – `CreateAuctionWithNewItemDto` arrives via `AuctionsController.CreateWithNewItem`. The DTO is mapped to an `Auction` with nested `Item` entity. The controller sets new GUIDs, validates time bounds, persists the aggregate, then re-hydrates it with joins for the response DTO.
2. **Schedule adjustments** – Before an auction starts, `PUT /api/auctions/{id}` can adjust timing and pricing. The controller verifies business rules, updates only allowed fields, stamps `UpdatedAt`, saves, and returns the fresh projection.
3. **Publishing items** – `ItemsController.Update` checks `dto.Publish`. When true and the current status is Draft, it flips to Published and stamps `PublishedAt`. Attempts to delete items engaged in auctions return `400 BadRequest` with explanatory messaging.

## Validation and error handling

- Timeline assertions (`EndsAt` > `StartsAt`, future `StartsAt`) ensure coherent scheduling.
- Pricing guardrails prevent negative values and disallow post-start modifications.
- Foreign-key constraints are enforced at both controller (pre-flight checks) and database level.
- Controllers use standard ASP.NET Core result types: `BadRequestObjectResult` with human-readable messages, `NotFound()`, and `CreatedAtAction`/`Ok` responses with DTO payloads.

## Seeding and environments

During application startup (`Program.cs`), the service applies pending migrations and optionally seeds sample data through `DbInitializer.InitDb`. In development environments (detected via `IHostEnvironment.IsDevelopment`), Swagger UI is enabled and the database receives a small set of items and auctions in various states to aid manual testing.

## Test coverage cues

The `AuctionService.Tests` project spins up a shared in-memory SQLite database per test class via `SqliteDbContextFactory`. Tests cover:
- Guardrails against duplicate auctions per item.
- Auction creation wiring of item foreign keys.
- Status and pricing mutation rules pre/post start.
- Item deletion and publish semantics.
- Lightweight list projections with optional item inclusion.

SQLite-specific conventions (JSON serialisation and `DateTimeOffset` converters) ensure parity with PostgreSQL behaviour while keeping tests fast and deterministic.

## Extending the service

Common extension points include:
- Adding read-only query endpoints (e.g., recent auctions, seller dashboards) by expanding controllers and AutoMapper projections.
- Introducing background jobs (e.g., to automatically mark auctions as ended) through hosted services or integration with a message bus.
- Enhancing `AuctionService.Application` with domain services for complex bidding or settlement logic.
- Instrumenting the API with logging/metrics/middleware for observability.

When extending, keep the one-auction-per-item invariant, update docs and tests accordingly, and consider new migration requirements.
