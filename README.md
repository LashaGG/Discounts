
### 1

Discounts.sln
│
├── Discounts.Domain/            # Entities, Enums, Constants (zero dependencies)
├── Discounts.Application/       # Interfaces, DTOs, Models, Validators, Mapping, HealthChecks
├── Discounts.Persistance/       # ApplicationDbContext, EF Core Migrations, Repository implementations
├── Discounts.Infrastructure/    # Service implementations, Background Workers, Identity, DI composition
├── Discounts.API/               # RESTful Web API (JWT auth, Swagger, HealthChecks endpoints)
├── Discounts.Web/               # ASP.NET Core MVC (Cookie auth, Views, Controllers)
└── Discounts.Tests/             # xUnit unit tests with Moq & FluentAssertions

### 2. Configure the Database Connection String

Open **both** `appsettings.json` files and update the `DefaultConnection` to match your SQL Server instance:

**`Discounts.API/appsettings.json`** and **`Discounts.Web/appsettings.json`**:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=DiscountsDb;Trusted_Connection=true;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

> **Example**: Replace `YOUR_SERVER_NAME` with `(localdb)\\mssqllocaldb`, `.\\SQLEXPRESS`, or your server name.

### 3. Apply EF Core Migrations (Create the Database)

Open the **Package Manager Console** in Visual Studio (`Tools → NuGet Package Manager → Package Manager Console`), set the **Default Project** to `Discounts.Persistance`, and run:

```powershell
Update-Database -Project Discounts.Persistance -StartupProject Discounts.API
```

Or via the .NET CLI from the solution root:

```bash
dotnet ef database update --project Discounts.Persistance --startup-project Discounts.API
```

> The database will be created automatically with all tables and schema.


### 5. Automatic Data Seeding

On first startup, the application automatically seeds:

- **Roles**: `Administrator`, `Merchant`, `Customer`
- **Default Admin Account** (see [Testing Credentials](#testing-credentials))
- **Categories** (Restaurants, Beauty & Health, Entertainment, Shops, Travel, Education, Services, Technology — in Georgian)
- **System Settings** (default reservation duration, etc.)

No manual seeding is required.

---

## How to Test the Application

### 🔌 API (Swagger)

1. Launch `Discounts.API` — Swagger UI opens at: `https://localhost:{port}/swagger`
2. **Authenticate**:
   - Call `POST /api/Auth/login` with admin credentials (see table below)
   - Copy the returned JWT token
   - Click the 🔒 **Authorize** button (top-right padlock icon)
   - Enter: `Bearer {your_token}` and click **Authorize**
3. All protected endpoints are now accessible with role-based authorization
4. **Health Checks**:
   - Liveness: `GET /health/live` — returns `200 OK` if the process is alive
   - Readiness: `GET /health/ready` — runs all registered checks (SQL Server, Worker Services)

### 🌐 Web MVC

1. Launch `Discounts.Web` — the home page opens at: `https://localhost:{port}/`
2. **Register** a new Merchant or Customer account, or **Log in** with the seeded Admin
3. **Flows to test**:

| Role | Key Actions |
|---|---|
| **Administrator** | Approve/Reject pending merchant offers · Manage users (CRUD) · Configure global settings (booking duration) |
| **Merchant** | Create new discount offers (pending approval) · View active/expired offer statistics · Edit offers within admin-defined limits · View sales history |
| **Customer** | Browse & search available discounts · Book (temporarily reserve) a coupon · Purchase coupons · View "My Coupons" dashboard |

### ⚙️ Background Worker Services

Two `BackgroundService` workers run automatically in the background — **no manual intervention required**:

| Service | Interval | Purpose |
|---|---|---|
| `ReservationCleanupService` | Every **3 minutes** | Releases bookings that exceed the admin-configured reservation time limit |
| `OfferExpirationService` | Every **10 minutes** | Marks expired discounts as inactive and updates coupon statuses |

Both workers report health status to the Health Check system and include structured error logging with retry logic.

---

## Testing Credentials

| **Administrator** | `admin@gmail.com` | `Aa123123@` |
