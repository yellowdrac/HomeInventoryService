# HomeInventory — Backend

Backend for **HomeInventory**, a multi-tenant home inventory management application.

* **.NET 10 (LTS)** · **C# 14** · **ASP.NET Core Minimal APIs**
* **EF Core 10** · **PostgreSQL** via **Npgsql 10**
* **Clean Architecture** (4 layers) · **MediatR** (CQRS) · **FluentValidation**
* **JWT** authentication · **AWS S3** for item photos

---

## Phases

The backend was built incrementally. Each phase is a self-contained, shipped slice of functionality (one ticket / branch per phase). All phases below are implemented and merged into `main`.

| Phase | Theme | Status | Highlights |
| ----- | ----- | ------ | ---------- |
| **0** | Foundation | ✅ | Solution skeleton, domain model, persistence wiring, initial migration, `/health` endpoint |
| **1** | Authentication & Identity | ✅ | JWT register / login / refresh, password hashing, `ICurrentUser` from claims |
| **2** | Households & Locations | ✅ | Household tenancy, join codes, hierarchical locations with QR slugs |
| **3** | Items & Stock | ✅ | Item catalog, stock lots, add / consume / discard stock |
| **4** | Stock Movements | ✅ | Move stock between locations, movement audit log |
| **5** | Search & Inventory Lookup | ✅ | Spanish fuzzy search (trigram), location contents |
| **6** | Printables, Expirations & Photos | ✅ | Printable QR labels, expiring-stock / kitchen views, S3 item photos |
| **7** | Dashboard | ✅ | Aggregated household summary metrics |
| **8** | Containerization & Deployment | ✅ | Dockerfile, docker-compose, runtime config, CORS for deployment |

### Phase 0 — Foundation

Skeleton + domain model + persistence wiring + initial migration + health endpoint.

* 4-layer Clean Architecture solution (Domain / Application / Infrastructure / Api).
* Domain entities (`Household`, `Location`, `Item`, `StockLot`, `Movement`) and enums.
* EF Core configurations, `ApplicationDbContext`, design-time factory, `InitialCreate` migration.
* `unaccent` + `pg_trgm` extensions and the GIN trigram index for fuzzy search.
* `/health` endpoint with a database check.

### Phase 1 — Authentication & Identity

JWT-based authentication. Adds the `AddIdentity` migration and the `Jwt` configuration section.

| Method | Route | Description |
| ------ | ----- | ----------- |
| `POST` | `/api/auth/register` | Create a user account |
| `POST` | `/api/auth/login` | Exchange credentials for access + refresh tokens |
| `POST` | `/api/auth/refresh` | Rotate the access token using a refresh token |

`ICurrentUser` is now resolved from JWT claims; all feature endpoints below `RequireAuthorization()`.

### Phase 2 — Households & Locations

| Method | Route | Description |
| ------ | ----- | ----------- |
| `POST` | `/api/households` | Create a household (becomes the tenant root) |
| `POST` | `/api/households/join` | Join an existing household via join code |
| `GET`  | `/api/households/me` | Get the current user's household |
| `POST` | `/api/households/regenerate-code` | Regenerate the household join code |
| `GET`  | `/api/locations/tree` | Hierarchical location tree |
| `GET`  | `/api/locations/{id}` | Get a location by id |
| `GET`  | `/api/locations/by-slug/{slug}` | Resolve a location by its QR slug |
| `POST` | `/api/locations` | Create a location |
| `PUT`  | `/api/locations/{id}` | Update a location |
| `POST` | `/api/locations/{id}/move` | Re-parent a location |
| `DELETE` | `/api/locations/{id}` | Delete a location |

### Phase 3 — Items & Stock

| Method | Route | Description |
| ------ | ----- | ----------- |
| `GET`  | `/api/items` | List items |
| `GET`  | `/api/items/{id}` | Get an item |
| `POST` | `/api/items` | Create an item |
| `PUT`  | `/api/items/{id}` | Update an item |
| `DELETE` | `/api/items/{id}` | Delete an item |
| `POST` | `/api/items/{itemId}/stock` | Add a stock lot for an item |
| `PUT`  | `/api/stock-lots/{id}` | Update a stock lot |
| `DELETE` | `/api/stock-lots/{id}` | Delete a stock lot |
| `POST` | `/api/stock-lots/{id}/consume` | Consume from a stock lot |
| `POST` | `/api/stock-lots/{id}/discard` | Discard from a stock lot |

### Phase 4 — Stock Movements

| Method | Route | Description |
| ------ | ----- | ----------- |
| `POST` | `/api/stock-lots/{id}/move` | Move stock to another location |
| `GET`  | `/api/movements` | Inventory movement audit log |
| `GET`  | `/api/locations/{id}/contents` | List stock held in a location |

### Phase 5 — Search & Inventory Lookup

| Method | Route | Description |
| ------ | ----- | ----------- |
| `GET`  | `/api/search` | Spanish-language fuzzy search across the inventory (GIN trigram on `Items.NormalizedName`) |

### Phase 6 — Printables, Expirations & Photos

| Method | Route | Description |
| ------ | ----- | ----------- |
| `GET`  | `/api/locations/printable` | Printable QR labels (optionally scoped to a subtree) |
| `GET`  | `/api/expirations` | Expiring stock |
| `POST` | `/api/expirations/discard-expired` | Discard all expired stock |
| `GET`  | `/api/kitchen/overview` | Kitchen-focused expiration overview |
| `POST` | `/api/items/{id}/photo` | Upload an item photo to S3 |
| `DELETE` | `/api/items/{id}/photo` | Delete an item photo |

Item photos are stored in **AWS S3**: `Item.PhotoUrl` holds the S3 object key, and read DTOs return short-lived presigned URLs. Requires the `Storage:S3` configuration section (see [Configuration](#configuration)).

### Phase 7 — Dashboard

| Method | Route | Description |
| ------ | ----- | ----------- |
| `GET`  | `/api/dashboard` | Aggregated household summary (counts, stock, expirations) |

### Phase 8 — Containerization & Deployment

* `Dockerfile` — multi-stage build (SDK → ASP.NET runtime), runs as the non-root `app` user, honors `$PORT` (Render / Cloud Run convention, default `8080`).
* `docker-compose.yml` — local `postgres` + `backend` stack with migrations applied on startup.
* No secrets baked into the image; the connection string, JWT signing key and S3 credentials are injected as environment variables at runtime.
* CORS configured from `Cors:AllowedOrigins` for the deployed frontend.

See [Running with Docker](#running-with-docker).

---

## Solution Structure

```text
backend/
├─ HomeInventory.sln
├─ Directory.Build.props          # net10.0, Nullable, ImplicitUsings, LangVersion latest
├─ global.json                    # pins the .NET 10 SDK
├─ Dockerfile                     # multi-stage build → ASP.NET runtime image
├─ docker-compose.yml             # local postgres + backend stack
├─ HomeInventory.Domain/          # pure POCOs + enums (no dependencies)
├─ HomeInventory.Application/     # CQRS handlers, validators, contracts → references Domain only
├─ HomeInventory.Infrastructure/  # EF Core, configs, migrations, identity, S3 → references Application
├─ HomeInventory.Api/             # composition root: Program.cs, endpoints, Swagger, CORS, auth
└─ HomeInventory.Application.UnitTests/   # xUnit + NSubstitute + FluentAssertions
```

### Dependency Rules (Strict)

```text
Api ──> Application <── Infrastructure
 │                          │
 └────> Infrastructure      └──> Application ──> Domain
```

* **Domain** references nothing (no projects, no frameworks).
* **Application** references only Domain.
* **Infrastructure** references Application.
* **Api** references Application + Infrastructure (the only place with concrete DI registrations).

---

## Prerequisites

* **.NET 10 SDK** (10.0.300 or later). Verify with:

```bash
dotnet --version
```

If your default `dotnet` version is different, this repository pins the SDK through `global.json`.

* **PostgreSQL** accessible locally (tested with PostgreSQL 18).
* **dotnet-ef** 10 tool:

```bash
dotnet tool install --global dotnet-ef --version 10.0.*
```

* *(Optional)* **Docker** + **Docker Compose** to run the full stack in containers.
* *(Optional, for Phase 6 photos)* An **AWS S3** bucket and IAM credentials.

---

## Configuration

### Connection string

Read from `ConnectionStrings:Default`. During development it lives in
`HomeInventory.Api/appsettings.Development.json`:

```json
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=homeinventory;Username=postgres;Password=postgres"
}
```

To avoid committing real credentials, use **User Secrets** (already enabled in the Api project):

```bash
cd HomeInventory.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=homeinventory;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
```

### JWT (Phase 1)

The `Jwt` section is in `appsettings.json`; the **signing key is a secret** and must be supplied separately (≥ 32 chars):

```json
"Jwt": {
  "Issuer": "HomeInventory",
  "Audience": "HomeInventoryClient",
  "AccessTokenMinutes": 15,
  "RefreshTokenDays": 7
}
```

```bash
dotnet user-secrets set "Jwt:SigningKey" "a-long-random-development-signing-key-min-32-chars"
```

### S3 item photos (Phase 6)

Required only if you use the photo endpoints. Set the `Storage:S3` section:

```bash
dotnet user-secrets set "Storage:S3:BucketName"      "your-bucket"
dotnet user-secrets set "Storage:S3:Region"          "us-east-1"
dotnet user-secrets set "Storage:S3:AccessKeyId"     "..."
dotnet user-secrets set "Storage:S3:SecretAccessKey" "..."
```

---

## Getting Started

### 1. Create the Database

`dotnet ef database update` creates the database if it does not already exist. To create it manually:

```sql
CREATE DATABASE homeinventory;
```

> The `unaccent` and `pg_trgm` extensions are created by the initial migration (`InitialCreate`), so the database user must have permission to execute `CREATE EXTENSION` (superuser or equivalent privileges).

### 2. Apply Migrations

From the `backend/` directory:

```bash
dotnet ef database update -p HomeInventory.Infrastructure -s HomeInventory.Infrastructure
```

This applies all migrations (`InitialCreate`, `AddIdentity`), installs the `unaccent` and `pg_trgm` extensions, and creates the **GIN trigram index** on `Items.NormalizedName` for Spanish-language fuzzy search.

> The project includes an `IDesignTimeDbContextFactory` (`ApplicationDbContextFactory`), so `dotnet ef` works using Infrastructure as the startup project.
> You can override the design-time connection string using the `HOMEINVENTORY_CONNECTION` environment variable.

### 3. Run the API

```bash
dotnet run --project HomeInventory.Api
```

The API listens on port **5080** in development:

* Health check: http://localhost:5080/health
* Swagger UI (Development only): http://localhost:5080/swagger — includes a **Bearer** auth scheme; paste an access token to call protected endpoints.

CORS allows the frontend development origin:

```text
http://localhost:3000
```

### 4. Verify the Health Check

```bash
curl http://localhost:5080/health
```

Expected response (database connected):

```json
{
  "status": "Healthy",
  "database": "Healthy",
  "totalDuration": 12.34,
  "timestamp": "2026-06-02T00:00:00.0000000+00:00",
  "version": "1.0.0.0"
}
```

---

## Running with Docker

The compose stack starts PostgreSQL and the backend (migrations applied on startup) and exposes the API on **8080**:

```bash
docker compose up --build
```

* API: http://localhost:8080  (health at `/health`)
* The frontend lives in a separate repository (deploys to Vercel); run it separately and point it at `http://localhost:8080`.

Runtime configuration is supplied via environment variables (double-underscore maps to nested keys):

| Variable | Purpose |
| -------- | ------- |
| `ConnectionStrings__Default` | PostgreSQL connection string |
| `Jwt__SigningKey` | JWT signing key (≥ 32 chars) |
| `Storage__S3__*` | S3 bucket / region / credentials (photos) |
| `Cors__AllowedOrigins` | Comma-separated allowed origins |
| `RUN_MIGRATIONS_ON_STARTUP` | `true` to apply migrations on boot |
| `PORT` | Listening port (default `8080`) |

> The values in `docker-compose.yml` are **local development defaults only** — never put production secrets there.

---

## Domain Model

| Entity      | Notes                                                                       |
| ----------- | --------------------------------------------------------------------------- |
| `Household` | Root tenant. Unique `JoinCode`.                                             |
| `Location`  | Hierarchical structure (self-referencing FK, `Restrict`). Unique `QrSlug`.  |
| `Item`      | `NormalizedName` unique per household + GIN trigram index for fuzzy search. `PhotoUrl` holds the S3 object key. |
| `StockLot`  | Inventory stock for an item in a location (quantity and dates).             |
| `Movement`  | Inventory movement audit log.                                               |

Enums: `LocationType`, `TrackingType`, and `MovementType` (stored as text).

`BaseEntity` provides `Id`, `CreatedAt`, and `UpdatedAt`. Multi-tenant entities implement `IHouseholdScoped` and are globally filtered by `HouseholdId` through a query filter.

### Multi-Tenancy

Authentication is implemented (Phase 1). `ICurrentUser` resolves `UserId` and `HouseholdId` from the JWT claims of the incoming request, and the `HouseholdId` query filter scopes every tenant query automatically.

---

## Testing

Unit tests use **xUnit**, **NSubstitute**, and **FluentAssertions** (`HomeInventory.Application.UnitTests`):

```bash
dotnet test
```

---

## Useful Commands

```bash
# Build the entire solution
dotnet build

# Run the test suite
dotnet test

# Create a new migration
dotnet ef migrations add <MigrationName> -p HomeInventory.Infrastructure -s HomeInventory.Infrastructure -o Persistence/Migrations

# Remove the last migration (before applying it)
dotnet ef migrations remove -p HomeInventory.Infrastructure -s HomeInventory.Infrastructure
```
