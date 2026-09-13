# Noba Car Rental System

A modern, enterprise-grade car rental management system built with **.NET 10**, **.NET Aspire**, **Microsoft SQL Server**, **Entity Framework Core**, and **MudBlazor**.

Designed and structured according to enterprise **Clean Architecture** and **Domain-Driven Design (DDD)** principles, prioritizing testability, domain isolation, separation of concerns, concurrency control, and the Open/Closed Principle.

---

## 1. Business Specification & Domain Rules

The system manages physical fleet assets, multi-station logistics, and end-to-end vehicle rental lifecycles across Norway:

### 1.1 Vehicle Categories & Extensibility
Cars for rent are categorized into three initial groups:
- **Small car**
- **Combi**
- **Truck**

More may be added later.

### 1.2 Rental Rates & Pricing Formulas (in NOK)
All rates and calculated totals are explicitly denoted in **Norwegian Krone (NOK)** with two decimal places (`decimal(18, 2)`):
- `baseDayRental`: Base daily rental cost (NOK/day)
- `baseKmPrice`: Base kilometer price (NOK/km)

| Category | Pricing Formula (NOK) | Notes |
|---|---|---|
| **Small car** | `Price = baseDayRental * numberOfDays` | Free mileage (driven km not billed) |
| **Combi** | `Price = (baseDayRental * numberOfDays * 1.3) + (baseKmPrice * numberOfKm)` | 30% day rate surcharge + standard km charge |
| **Truck** | `Price = (baseDayRental * numberOfDays * 1.5) + (baseKmPrice * numberOfKm * 1.5)` | 50% day rate surcharge + 50% km rate surcharge |

### 1.3 Fleet & Station Logistics
- **Permanent Vehicle Assets**: Vehicles are registered physical assets with immutable categories and strictly monotonically increasing odometers.
- **Multi-Station Network & One-Way Rentals**:
  - Seeded across 5 major Norwegian hubs: **OSL** (Oslo Airport Gardermoen), **BGO** (Bergen Airport Flesland), **TRD** (Trondheim Airport Værnes), **SVG** (Stavanger Airport Sola), and **OSLO-C** (Oslo Central Station).
  - Pickups occur at the station where the vehicle is currently located.
  - Returns can be made at **any active station** (one-way rentals supported). Upon return completion, the vehicle is automatically relocated to the return station.
- **Vehicle Status Lifecycle**: `Available` $\leftrightarrow$ `Rented`, `Maintenance`, or `Decommissioned` (soft-deleted).

### 1.4 Core Rental Workflows
1. **Registration of Car Pickup**:
   - Booking number: **Auto-generated** sequential identifier (`long BookingNumber` via database `IDENTITY(1, 1)`).
   - Pickup station: Selection filters available vehicles physically present at that hub.
   - Available vehicle selection: Automatically locks vehicle category and current odometer reading to prevent tampering, typos, or billing fraud.
   - Customer SSN: Validated as exactly 11 digits (masked as `****** 78901` in API responses); it is not available as a booking search field.
   - Server-Authoritative Tariffs: Tariffs (`BaseDayRental` and `BaseKmPrice`) are configured per vehicle during fleet registration and resolved strictly on the backend. `RegisterPickupRequest` contains zero client-dictated pricing fields. For small cars, `BaseKmPrice` is strictly enforced to `0.00 NOK`.
2. **Registration of Returned Car**:
   - Single RESTful route: `POST /api/v1/rentals/{bookingNumber}/return`.
   - Route path specifies the booking identity; payload contains return station, return date/time, and return odometer reading (`returnKm >= pickupKm`).
   - Concurrency precondition: Client must pass the entity's `RowVersion` via the standard HTTP `If-Match` header. Missing headers return `428 Precondition Required`; stale headers return `412 Precondition Failed`.
   - **Outcome**: Automatic computation of billed days, kilometers driven, vehicle relocation to return station, and final price breakdown in NOK according to category formulas. Response returns updated `ETag`.

---

## 2. Architectural Assumptions & Enterprise Patterns

1. **Billing Days Calculation (`numberOfDays`)**:
   - Billed in 24-hour periods, rounded up using ceiling, with a minimum billing of 1 full day:
     $$\text{numberOfDays} = \max(1, \lceil(\text{returnDateTime} - \text{pickupDateTime}).\text{TotalDays}\rceil)$$
2. **Authoritative Tariffs & Immutability**:
   - Vehicle rates are maintained on `CarEntity` and snapshotted onto `RentalBookingEntity` during pickup, ensuring tamper-proof historical billing records.
3. **Automated EF Core Soft-Delete & Global Query Filters**:
   - Behavioral interfaces `ISoftDeletable` and `IAuditableEntity` paired with layered base classes `AuditableEntity` and `SoftDeletableEntity`.
   - `DbContext.SaveChangesAsync` automatically intercepts `EntityState.Deleted` on soft-deletable entities, mutating them to `EntityState.Modified`, setting `IsDeleted = true` and `DeletedAt = timeProvider.GetUtcNow()`.
   - Global query filters (`WHERE [t].[IsDeleted] = 0`) are automatically applied to all queries. Auditing or administrative queries can opt out via `.IgnoreQueryFilters()`.
   - Foreign-key navigations to soft-deletable entities are configured with `.IsRequired(false)` to prevent EF Core 10622 warnings and allow historical rental bookings to load even if their vehicle or station was decommissioned.
4. **Optimistic Concurrency & HTTP REST Semantics**:
   - `RowVersion` timestamp tokens on `CarEntity`, `StationEntity`, and `RentalBookingEntity`.
   - Database-level unique filtered index on active bookings: `CREATE UNIQUE INDEX [IX_RentalBooking_RegistrationNumber] ON [RentalBooking] ([RegistrationNumber]) WHERE [Status] = 1` preventing double-rental race conditions.
   - HTTP `ETag` response headers emitted on entity retrieval, pickup, and return.
   - `If-Match` header evaluation on mutating transitions: returns RFC 9110 **`412 Precondition Failed`** if stale.
   - Conditional GETs with `If-None-Match` return **`304 Not Modified`**.
5. **Centralized Error Handling (RFC 7807)**:
   - Application-wide `GlobalExceptionHandler` implementing ASP.NET Core `IExceptionHandler`.
   - Maps domain exceptions (`BookingNotFoundException` -> 404, `PreconditionFailedException` -> 412, `RentalConcurrencyException` -> 409, `RentalValidationException` -> 400) to standard RFC 7807 `ProblemDetails` without controller try-catch boilerplate.
6. **Built-in Rate Limiting (`Microsoft.AspNetCore.RateLimiting`)**:
   - Sliding window partition limiter: 100 permits/min for authenticated M2M clients (keyed by `client_id` claim), 30 permits/min for unauthenticated endpoints (keyed by IP).
   - Rate limit exhaustion yields **`429 Too Many Requests`** with standard `Retry-After` header.
7. **Server-Side Pagination & Dynamic Sorting**:
   - Domain `PagedResult<T>` model integrated with MudBlazor `ServerData="ServerReload"`, offloading paging, search, and sorting to SQL queries.
8. **Auditing with `TimeProvider`**:
   - Fully testable, deterministic date/time operations via .NET `TimeProvider` (no untestable `DateTime.UtcNow` or `DateTime.Now`).

---

## 3. Clean Architecture & Solution Structure

```
CarRental/
├── NobaRental-AppHost/                             # .NET Aspire orchestration
│   └── AppHost.cs                                 # Coordinates WebApi, Frontend & Health Checks
├── NobaRental-Backend/
│   ├── build/                                     # Deployment pipelines (Azure DevOps / CI/CD YAMLs)
│   │   ├── azure-pipelines-pr.yaml                # PR pipeline: build, analyzers, tests, Bicep validation
│   │   └── azure-pipelines-ci.yaml                # CI/CD pipeline: build, test, package, infra & DB migration
│   ├── infra/                                     # Azure Infrastructure as Code (Bicep)
│   │   ├── main.bicep                             # Core template: App Service, Azure SQL, Key Vault, App Insights
│   │   ├── bicepconfig.json                       # Bicep analyzer & linter rules
│   │   └── parameters/                            # Environment parameter files (dev, test, prod)
│   └── src/
│       ├── NobaRental.Backend.Domain/             # Core domain abstractions, models & exceptions
│       │   ├── ICarFleetApi.cs                    # Car fleet business contract
│       │   ├── IRentalBookingApi.cs               # Rental booking business contract
│       │   ├── IStationApi.cs                     # Station management business contract
│       │   ├── Models/                            # Sealed records: Car, Station, RentalBooking, PagedResult<T>
│       │   ├── Values/                            # Enums: CarCategory, CarStatus, RentalStatus
│       │   └── Exceptions/                        # RentalConcurrencyException, StationInUseException, etc.
│       ├── NobaRental.Backend.Business/           # Core business logic & pricing calculations
│       │   ├── Api/CarFleetApi.cs                 # Car fleet service implementation
│       │   ├── Api/RentalBookingApi.cs            # Rental booking service implementation
│       │   ├── Api/StationApi.cs                  # Station management service implementation
│       │   ├── Pricing/RentalPriceCalculator.cs   # Strategy-driven pricing engine adhering to OCP
│       │   ├── Pricing/Strategies/                # SmallCar, Combi, Truck pricing strategy implementations
│       │   ├── Pricing/RentalDurationCalculator.cs# Billed days & km delta calculation
│       │   └── Mapping/                           # Entity <-> Domain mappers (CarMap, StationMap, RentalBookingMap)
│       ├── NobaRental.Backend.Business.Test/      # Unit tests for domain APIs, pricing & fleet rules (76 tests)
│       ├── NobaRental.Backend.Data/               # EF Core persistence
│       │   ├── Entities/                          # CarEntity, StationEntity, RentalBookingEntity
│       │   ├── Entities/Common/                   # IAuditableEntity, ISoftDeletable, AuditableEntity, SoftDeletableEntity
│       │   ├── Configurations/                    # Fluent entity configurations, query filters & indexes
│       │   ├── Migrations/                        # Generated EF Core migrations with seed data
│       │   └── NobaRentalDbContext.cs             # DbContext with query filters, interceptors & unicode conventions
│       ├── NobaRental.Backend.Data.MigrationStartup/ # Dedicated EF Core design-time migration host
│       │   └── Program.cs                         # Configures DbContext for EF Core CLI tools
│       ├── NobaRental.Backend.Data.Test/          # EF Core metadata, soft-delete & SQL Server tests (12 tests)
│       ├── NobaRental.Backend.WebApi/             # ASP.NET Core REST API
│       │   ├── Auth/                              # AuthConstants (scopes & policy names)
│       │   ├── Controllers/CarsController.cs      # Fleet REST API [Authorize(FleetManage / RentalsRead)]
│       │   ├── Controllers/StationsController.cs  # Station REST API [Authorize(FleetManage / RentalsRead)]
│       │   ├── Controllers/RentalBookingsController.cs # Rental REST API [Authorize(RentalsPickup / RentalsReturn / RentalsRead)]
│       │   ├── Helpers/ETagHelper.cs              # ETag generation, If-Match & If-None-Match header parsing
│       │   ├── Middleware/GlobalExceptionHandler.cs # Centralized RFC 7807 ProblemDetails exception handling
│       │   ├── Startups/                          # AuthenticationStartup, RateLimitingStartup, ValidationStartup, SwaggerStartup, HealthStartup
│       │   ├── Validators/                        # FluentValidation request validators
│       │   └── Mapping/                           # Domain Models <-> Client Response DTOs & Enum Value Mappers
│       ├── NobaRental.Backend.WebApi.Client/      # Shared Client library
│       │   ├── ICarApiClient.cs                   # RestEase typed client for cars
│       │   ├── IStationApiClient.cs               # RestEase typed client for stations
│       │   ├── IRentalBookingApiClient.cs         # RestEase typed client for rentals & price estimates
│       │   ├── Models/Request/                    # DTO request records (RegisterPickupRequest, ReturnRentalRequest, etc.)
│       │   ├── Models/Response/                   # DTO response records & PagedResultResponse<T>
│       │   └── ServiceCollectionExtensions.cs     # Typed client DI registration
│       └── NobaRental.Backend.WebApi.Client.Test/ # WebApplicationFactory integration, auth, rate-limiting & concurrency tests (52 tests)
├── NobaRental-Frontend/
│   └── src/
│       ├── NobaRental.Frontend.Server/            # Blazor Server host with YARP reverse proxy & NavMenu
│       └── NobaRental.Frontend.Client/            # Interactive Blazor UI with MudBlazor components
│           ├── Pages/Cars/                        # CarsPage (table, server reload, decommission), AddCarDialog
│           ├── Pages/Stations/                    # StationsPage (table, CRUD), StationDialog
│           └── Pages/Rentals/                     # RentalsPage (server reload, routes), PickupDialog, ReturnDialog
└── NobaRental-Shared/
    └── NobaRental.Shared.ServiceDefaults/        # Observability, OpenTelemetry, health checks
```

---

## 4. Local Setup & Execution Guide

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Microsoft SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (Developer Edition or LocalDB)
- Optional: [EF Core Tools](https://learn.microsoft.com/ef/core/cli/dotnet) (`dotnet tool install --global dotnet-ef`)

---

### Step 1: Database Setup & Migration

The connection string in `NobaRental-Backend/src/NobaRental.Backend.WebApi/appsettings.json` points to local SQL Server:
```json
"ConnectionStrings": {
  "NobaRental": "Server=.;Database=NobaRental;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

To apply the migration and automatically seed the initial 5 stations (`OSL`, `BGO`, `TRD`, `SVG`, `OSLO-C`) and 3 fleet vehicles (`EV12345`, `BT20001`, `TR99001`):

```powershell
dotnet ef database update --project NobaRental-Backend/src/NobaRental.Backend.Data/NobaRental.Backend.Data.csproj --startup-project NobaRental-Backend/src/NobaRental.Backend.Data.MigrationStartup/NobaRental.Backend.Data.MigrationStartup.csproj
```

*(Alternatively, run `Update-Database` in Visual Studio Package Manager Console).*

---

### Step 2: Run the Application via .NET Aspire

Launch the entire distributed application (Backend API, Swagger, and Blazor Frontend) orchestrated via .NET Aspire:

```powershell
dotnet run --project NobaRental-AppHost/NobaRental.AppHost.csproj
```

The Aspire dashboard will start and display live status, logs, and endpoints:
- **Blazor Frontend UI**: `https://localhost:7001` (or assigned port)
  - **Dashboard**: Overview metrics.
  - **Fleet (`/cars`)**: Vehicle inventory, status filtering, odometer tracking, and decommission action.
  - **Bookings (`/rentals`)**: Active rentals, one-way route chips (`OSL ➔ BGO`), pickup and return modals.
  - **Stations (`/stations`)**: Hub management, station creation, editing, and deletion.
- **Backend Swagger UI**: `https://localhost:7000/swagger`

---

### Step 3: Running Automated Tests

Run the full automated test suite across all layers:

```powershell
dotnet test NobaRental.slnx
```

---

## 5. REST API Reference

All endpoints are versioned and return RFC 7807 Problem Details on validation or business errors:

### Station Endpoints
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/v1/stations` | Retrieve all stations (optional `includeInactive` filter) |
| `GET` | `/api/v1/stations/{code}` | Retrieve station by uppercase code |
| `POST` | `/api/v1/stations` | Create a new rental station |
| `PUT` | `/api/v1/stations/{code}` | Update station details, name, or active status |
| `DELETE` | `/api/v1/stations/{code}` | Soft-delete station (protected against active fleet assignment) |

### Car Fleet Endpoints
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/v1/cars` | Paginated car fleet search with sorting and station/status filters |
| `GET` | `/api/v1/cars/available` | List available vehicles (filtered by station and category) |
| `GET` | `/api/v1/cars/{registrationNumber}` | Retrieve vehicle details by license plate |
| `POST` | `/api/v1/cars` | Register a new vehicle to the fleet |
| `PUT` | `/api/v1/cars/{registrationNumber}/tariff` | Update vehicle commercial tariffs (supports mid-rental update for future rentals; protected by `If-Match`) |
| `DELETE` | `/api/v1/cars/{registrationNumber}` | Decommission vehicle (soft-delete) |

### Rental Booking Endpoints
| Method | Endpoint | Description | Headers / Concurrency |
|---|---|---|---|
| `POST` | `/api/v1/rentals/pickup` | Register vehicle pickup with station and SSN (resolves vehicle tariffs server-side) | Emits `ETag` (`201 Created`) |
| `POST` | `/api/v1/rentals/{bookingNumber}/return` | Register vehicle return, calculate totals, and relocate vehicle | Requires `If-Match` ETag (`428` when missing, `412` when stale; emits new `ETag` on `200 OK`) |
| `POST` | `/api/v1/rentals/estimate-price` | Single Source of Truth: estimate price and duration in advance of return | - |
| `GET` | `/api/v1/rentals/{bookingNumber}` | Retrieve specific rental booking by unique booking number | Supports `If-None-Match` (`304 Not Modified`); emits `ETag` |
| `GET` | `/api/v1/rentals` | Paginated booking records with sorting and filters | - |
| `GET` | `/api/v1/rentals/active` | Retrieve currently active ongoing rentals | - |
| `GET` | `/health` | Health check endpoint reporting backend and SQL database readiness | - |

---

## 6. Authentication & Authorization (Auth0 Machine-to-Machine)

The system implements enterprise OAuth2 Machine-to-Machine (M2M) authentication and scope-based authorization with **Auth0**, modeled after the architecture used in `NobbKontrakt`:

```
                                  [Auth0 Tenant]
                     https://dev-nobarental.eu.auth0.com/
                                     ^
                                     | 1. Client Credentials Grant
                                     |    (client_id, client_secret, audience, scope)
                                     v
+--------------------------------------------------------------------------------+
| NobaRental-Frontend.Server                                                     |
|                                                                                |
|  [TokenProvider]                                                               |
|    - Requests JWT from Auth0 oauth/token                                       |
|    - Caches token in HybridCache (refreshes 5 mins before expiry)              |
|                                                                                |
|  [BearerTokenHandler]                  [YARP ReverseProxy]                     |
|    - Injects Authorization: Bearer       - Injects Authorization: Bearer       |
|      into typed RestEase client            into proxied /api/v1 calls          |
+-----------------------------------+--------------------------------------------+
                                    |
                                    | 2. HTTP Requests with Bearer <JWT>
                                    v
+--------------------------------------------------------------------------------+
| NobaRental-Backend.WebApi (Resource Server)                                    |
|                                                                                |
|  [AuthenticationStartup]                                                       |
|    - AddAuthentication(JwtBearerDefaults.AuthenticationScheme)                 |
|    - AddJwtBearer(Authority, Audience)                                         |
|    - AddAuthorization with Scopes / Permissions Policies                       |
|                                                                                |
|  [Controllers & Endpoints]                                                     |
|    - [Authorize(Policies.RentalsPickup)] -> rentals:pickup                     |
|    - [Authorize(Policies.RentalsReturn)] -> rentals:return                     |
|    - [Authorize(Policies.RentalsRead)]   -> rentals:read                       |
|    - [Authorize(Policies.FleetManage)]   -> fleet:manage                       |
+--------------------------------------------------------------------------------+
```

### 6.1 Scopes & Authorization Policies

Fine-grained permissions enforce least-privilege access across all controller actions:

| Scope (Permission) | Authorization Policy | Applied Endpoints |
|---|---|---|
| `rentals:pickup` | `RentalsPickup` | `POST /api/v1/rentals/pickup` |
| `rentals:return` | `RentalsReturn` | `POST /api/v1/rentals/{bookingNumber}/return` |
| `rentals:read` | `RentalsRead` | `GET /api/v1/rentals/**`, `POST /api/v1/rentals/estimate-price`, `GET /api/v1/cars/**`, `GET /api/v1/stations/**` |
| `fleet:manage` | `FleetManage` | `POST /api/v1/cars`, `DELETE /api/v1/cars/{reg}`, `POST /api/v1/stations`, `PUT /api/v1/stations/{code}`, `DELETE /api/v1/stations/{code}` |

> Public endpoints: `/api/v1/status` and `/health` remain open for health probes and load balancer liveness checks.

### 6.2 Token Management & Caching
- **Token Acquisition**: `TokenProvider` requests JWT tokens using the OAuth2 `client_credentials` grant against `https://dev-nobarental.eu.auth0.com/oauth/token`.
- **HybridCache Caching**: Tokens are cached in .NET 10's `HybridCache` with a 5-minute safety threshold before actual token expiration (`lifetime - 5 minutes`) to prevent edge-case 401s on in-flight requests.
- **Dual Injection Points**:
  1. **Blazor Server / C# Clients**: `BearerTokenHandler` (`DelegatingHandler`) automatically injects `Authorization: Bearer <token>` into typed RestEase client calls (`IRentalBookingApiClient`, `ICarApiClient`, `IStationApiClient`).
  2. **Blazor WebAssembly**: YARP Reverse Proxy request transform injects the Bearer token before forwarding `/api/**` calls from the browser to the backend.

### 6.3 Swagger UI Bearer Authentication
SwaggerGen is configured with OpenAPI Bearer authentication in `SwaggerStartup.cs`:
1. Acquire a token via curl:
   ```bash
   curl --request POST \
     --url https://dev-nobarental.eu.auth0.com/oauth/token \
     --header 'content-type: application/json' \
     --data '{
       "client_id": "fcRdx2LuqQzc232viukPNzqJ84peNu7Q",
       "client_secret": "6G_5SIn-IGWauphhAOsCn0JBPnQg_WQeH8F6h2HL7xcHpdpvbelcL2Z-vSKFmKjW",
       "audience": "https://api.nobarental.com",
       "grant_type": "client_credentials",
       "scope": "rentals:pickup rentals:return rentals:read fleet:manage"
     }'
   ```
2. Navigate to `https://localhost:7000/swagger`.
3. Click the **Authorize** button at the top right, paste the token, and click **Authorize**.
4. Test any protected endpoint directly in the browser.

### 6.4 Testing Without Auth0 Dependency
- **`TestAuthHandler`**: Custom test authentication handler plugged into `TestWebHost`, simulating an authenticated caller with full permissions so unit and integration tests run entirely offline with zero network latency.
- **`AuthorizationTests`**: Dedicated test suite verifying that unauthenticated requests to protected endpoints strictly return `401 Unauthorized`, while the status endpoint returns `200 OK`.

---

## 7. CI/CD Pipelines & Cloud Infrastructure (Build & Infra)

- **`build/azure-pipelines-pr.yaml`**: Strict PR validation running build with `TreatWarningsAsErrors`, all 137 unit/integration tests, and Bicep syntax validation.
- **`build/azure-pipelines-ci.yaml`**: Multi-stage release pipeline for building, bundling migrations (`efbundle`), deploying infrastructure via Bicep, executing database migrations, and deploying services to Azure App Service with slot swapping.
- **`infra/main.bicep`**: Declarative Infrastructure-as-Code for Azure App Service (Linux), Azure SQL, Azure Key Vault (Managed Identity references), and Application Insights.
