# HomeInventory — Backend (Phase 0)

Foundation of the **HomeInventory** backend, a home inventory management application.

* **.NET 10 (LTS)** · **C# 14** · **ASP.NET Core Web API**
* **EF Core 10** · **PostgreSQL** via **Npgsql 10**
* **Clean Architecture** with 4 layers

> Phase 0 = skeleton + domain model + persistence wiring + initial migration + health endpoint.
> **No** authentication, **no** feature endpoints, **no** business logic yet.

---

## Solution Structure

```text
backend/
├─ HomeInventory.sln
├─ Directory.Build.props          # net10.0, Nullable, ImplicitUsings, LangVersion latest
├─ global.json                    # pins the .NET 10 SDK
├─ HomeInventory.Domain/          # pure POCOs + enums (no dependencies)
├─ HomeInventory.Application/     # contracts (IApplicationDbContext, ICurrentUser) → references Domain only
├─ HomeInventory.Infrastructure/  # EF Core, configurations, migrations → references Application
└─ HomeInventory.Api/             # composition root: Program.cs, /health, Swagger, CORS
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

---

## Configuration

The connection string is read from `ConnectionStrings:Default`. During development it is stored in:

`HomeInventory.Api/appsettings.Development.json`

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

---

## Getting Started

### 1. Create the Database

`dotnet ef database update` creates the database if it does not already exist. If you prefer to create it manually:

```sql
CREATE DATABASE homeinventory;
```

> The `unaccent` and `pg_trgm` extensions are created by the initial migration (`InitialCreate`), so the database user must have permission to execute `CREATE EXTENSION` (superuser or equivalent privileges).

### 2. Apply the Initial Migration

From the `backend/` directory:

```bash
dotnet ef database update -p HomeInventory.Infrastructure -s HomeInventory.Infrastructure
```

This creates the full schema, installs the `unaccent` and `pg_trgm` extensions, and creates the **GIN trigram index** on `Items.NormalizedName` for Spanish-language fuzzy search.

> The project includes an `IDesignTimeDbContextFactory` (`ApplicationDbContextFactory`), so `dotnet ef` works using Infrastructure as the startup project.
> You can override the design-time connection string using the `HOMEINVENTORY_CONNECTION` environment variable.

### 3. Run the API

```bash
dotnet run --project HomeInventory.Api
```

The API listens on fixed port **5080**:

* Health check: http://localhost:5080/health
* Swagger UI (Development only): http://localhost:5080/swagger

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

## Domain Model

| Entity      | Notes                                                                       |
| ----------- | --------------------------------------------------------------------------- |
| `Household` | Root tenant. Unique `JoinCode`.                                             |
| `Location`  | Hierarchical structure (self-referencing FK, `Restrict`). Unique `QrSlug`.  |
| `Item`      | `NormalizedName` unique per household + GIN trigram index for fuzzy search. |
| `StockLot`  | Inventory stock for an item in a location (quantity and dates).             |
| `Movement`  | Inventory movement audit log.                                               |

Enums: `LocationType`, `TrackingType`, and `MovementType` (stored as text).

`BaseEntity` provides `Id`, `CreatedAt`, and `UpdatedAt`. Multi-tenant entities implement `IHouseholdScoped` and are globally filtered by `HouseholdId` through a query filter.

### Multi-Tenancy (Current State)

Authentication is not implemented yet. `ICurrentUser` is resolved through `CurrentUserStub`, which returns a fixed development `HouseholdId`.

**TODO Phase 1:** Replace with values extracted from JWT claims.

---

## Useful Commands

```bash
# Build the entire solution
dotnet build

# Create a new migration
dotnet ef migrations add <MigrationName> -p HomeInventory.Infrastructure -s HomeInventory.Infrastructure -o Persistence/Migrations

# Remove the last migration (before applying it)
dotnet ef migrations remove -p HomeInventory.Infrastructure -s HomeInventory.Infrastructure
```
