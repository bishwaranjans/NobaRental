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

> **Design Principle**: The architecture follows the **Open/Closed Principle (OCP)**. Additional vehicle categories can be introduced seamlessly by extending the pricing logic without altering database constraints or breaking active rentals.

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
   - Customer SSN: Validated 11-digit national identity number.
   - Rate snapshots (`baseDayRental` and `baseKmPrice` in NOK) locked onto the booking.
2. **Registration of Returned Car**:
   - Booking number & Return station.
   - Return date/time and return odometer reading (validated: `returnKm >= pickupKm`).
   - **Outcome**: Automatic computation of billed days, kilometers driven, vehicle relocation to return station, and final price breakdown in NOK according to category formulas.

---

## 2. Architectural Assumptions & Enterprise Patterns

1. **Billing Days Calculation (`numberOfDays`)**:
   - Billed in 24-hour periods, rounded up using ceiling, with a minimum billing of 1 full day:
     $$\text{numberOfDays} = \max(1, \lceil(\text{returnDateTime} - \text{pickupDateTime}).\text{TotalDays}\rceil)$$
2. **Rate Locking / Snapshotting**:
   - Rates are snapshotted at pickup time, protecting customers from mid-rental price changes and ensuring tamper-proof audit trails.
3. **Automated EF Core Soft-Delete & Global Query Filters**:
   - Behavioral interfaces `ISoftDeletable` and `IAuditableEntity` paired with layered base classes `AuditableEntity` and `SoftDeletableEntity`.
   - `DbContext.SaveChangesAsync` automatically intercepts `EntityState.Deleted` on soft-deletable entities, mutating them to `EntityState.Modified`, setting `IsDeleted = true` and `DeletedAt = timeProvider.GetUtcNow()`.
   - Global query filters (`WHERE [t].[IsDeleted] = 0`) are automatically applied to all queries. Auditing or administrative queries can opt out via `.IgnoreQueryFilters()`.
   - Foreign-key navigations to soft-deletable entities are configured with `.IsRequired(false)` to prevent EF Core 10622 warnings and allow historical rental bookings to load even if their vehicle or station was decommissioned.
4. **Optimistic Concurrency Control**:
   - `RowVersion` timestamp tokens on `CarEntity`, `StationEntity`, and `RentalBookingEntity`.
   - Concurrent updates trigger `DbUpdateConcurrencyException`, translated by the domain to `RentalConcurrencyException` and returned by the Web API as `HTTP 409 Conflict`.
5. **Server-Side Pagination & Dynamic Sorting**:
   - Domain `PagedResult<T>` model integrated with MudBlazor `ServerData="ServerReload"`, offloading paging, search, and sorting to SQL queries.
6. **Auditing with `TimeProvider`**:
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
│       │   ├── Pricing/RentalPriceCalculator.cs   # Category pricing formulas (in NOK)
│       │   ├── Pricing/RentalDurationCalculator.cs# Billed days & km delta calculation
│       │   └── Mapping/                           # Entity <-> Domain mappers (CarMap, StationMap, RentalBookingMap)
│       ├── NobaRental.Backend.Business.Test/      # Unit tests for domain APIs, pricing & fleet rules (75 tests)
│       ├── NobaRental.Backend.Data/               # EF Core persistence
│       │   ├── Entities/                          # CarEntity, StationEntity, RentalBookingEntity
│       │   ├── Entities/Common/                   # IAuditableEntity, ISoftDeletable, AuditableEntity, SoftDeletableEntity
│       │   ├── Configurations/                    # Fluent entity configurations, query filters & indexes
│       │   ├── Migrations/                        # Generated EF Core migrations with seed data
│       │   └── NobaRentalDbContext.cs             # DbContext with query filters, interceptors & unicode conventions
│       ├── NobaRental.Backend.Data.MigrationStartup/ # Dedicated EF Core design-time migration host
│       │   └── Program.cs                         # Configures DbContext for EF Core CLI tools
│       ├── NobaRental.Backend.Data.Test/          # EF Core metadata, soft-delete & SQL Server tests (6 tests)
│       ├── NobaRental.Backend.WebApi/             # ASP.NET Core REST API
│       │   ├── Controllers/CarsController.cs      # Fleet REST API
│       │   ├── Controllers/StationsController.cs  # Station REST API
│       │   ├── Controllers/RentalBookingsController.cs # Rental REST API
│       │   ├── Validators/                        # FluentValidation request validators
│       │   └── Mapping/                           # Domain Models <-> Client Response DTOs & Enum Value Mappers
│       ├── NobaRental.Backend.WebApi.Client/      # Shared Client library
│       │   ├── ICarApiClient.cs                   # RestEase typed client for cars
│       │   ├── IStationApiClient.cs               # RestEase typed client for stations
│       │   ├── IRentalBookingApiClient.cs         # RestEase typed client for rentals & price estimates
│       │   ├── Models/Request/                    # DTO request records
│       │   ├── Models/Response/                   # DTO response records & PagedResultResponse<T>
│       │   └── ServiceCollectionExtensions.cs     # Typed client DI registration
│       └── NobaRental.Backend.WebApi.Client.Test/ # WebApplicationFactory integration & enum mapping tests (49 tests)
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
| `DELETE` | `/api/v1/cars/{registrationNumber}` | Decommission vehicle (soft-delete) |

### Rental Booking Endpoints
| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/v1/rentals/pickup` | Register vehicle pickup with station, locked rates, and SSN |
| `POST` | `/api/v1/rentals/return` | Register vehicle return, calculate totals, and relocate vehicle |
| `POST` | `/api/v1/rentals/estimate-price` | Single Source of Truth: estimate price and duration in advance of return |
| `GET` | `/api/v1/rentals/{bookingNumber}` | Retrieve specific rental booking by unique booking number |
| `GET` | `/api/v1/rentals` | Paginated booking records with sorting and filters |
| `GET` | `/api/v1/rentals/active` | Retrieve currently active ongoing rentals |
| `GET` | `/health` | Health check endpoint reporting backend and SQL database readiness |

---

## 6. CI/CD Pipelines & Cloud Infrastructure (Build & Infra)

- **`build/azure-pipelines-pr.yaml`**: Strict PR validation running build with `TreatWarningsAsErrors`, all 130 unit/integration tests, and Bicep syntax validation.
- **`build/azure-pipelines-ci.yaml`**: Multi-stage release pipeline for building, bundling migrations (`efbundle`), deploying infrastructure via Bicep, executing database migrations, and deploying services to Azure App Service with slot swapping.
- **`infra/main.bicep`**: Declarative Infrastructure-as-Code for Azure App Service (Linux), Azure SQL, Azure Key Vault (Managed Identity references), and Application Insights.
