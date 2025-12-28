## Enterprise Leave Management

This is a small ASP.NET Core 8.0 MVC sample that demonstrates a multi‑role leave management system with basic security, logging, and caching.

Roles:
- Employee – applies for leave, views own leave summary and balances.
- Manager – reviews and approves/rejects pending employee leave.
- HR – sees all leave requests, overrides manager decisions, and views simple statistics.

---

## Setup Instructions

### Prerequisites
- .NET 8 SDK installed.
- SQL Server instance available (localdb or full SQL Server).

### 1. Configure the connection string
Edit `appsettings.json` (or `appsettings.Development.json`) in `EnterpriseLeaveManagement`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=EnterpriseLeaveManagement;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Replace `YOUR_SERVER` and other parts as appropriate for your environment.

### 2. Restore, build, and run
From the repository root:

```bash
cd EnterpriseLeaveManagement
dotnet restore
dotnet build
dotnet run
```

The app will:
- Apply EF Core migrations on first run (if configured via tooling).
- Seed default roles and demo users via `IdentitySeeder.SeedRolesAsync`.

Default demo users (password `Password@123`):
- Employee: `employee1@company.com`
- Manager: `manager1@company.com`
- HR: `hr1@company.com`

### 3. Log in and explore
- Browse to `https://localhost:{port}/`.
- Use one of the demo accounts above.
- After login, you are redirected based on role:
  - Employee → `EmployeeLeave/Index` (leave summary + list).
  - Manager → `ManagerLeave/Index` (pending approvals).
  - HR → `HrLeave/Index` (overview + statistics + override).

---

## Architecture Decisions

### Overall structure
- Single ASP.NET Core MVC project: `EnterpriseLeaveManagement`.
- Layers inside the project:
  - **UI layer** – MVC controllers (`Controllers`) and Razor views (`Views`).
  - **Application / domain layer** – services in `Services/*`, view models in `ViewModels/*`.
  - **Infrastructure layer** – EF Core `ApplicationDbContext`, repositories, and unit of work in `Infrastructure/*`, plus Identity and logging configuration.

While not a full multi‑project Clean Architecture, dependencies flow in a clean direction:
controllers → services → unit of work/repositories → EF Core.

### Repository + Unit of Work
- Generic `IRepository<TEntity>` and `Repository<TEntity>` encapsulate EF Core operations.
- `IUnitOfWork` exposes repositories (`LeaveRequests`, `LeaveStatusHistories`) and a single `SaveChangesAsync` method.
- `LeaveService` takes `IUnitOfWork` and works only with repositories and view models, not directly with `DbContext`. This keeps business logic testable and independent of UI concerns.

### Service layer
- `LeaveService` holds all leave‑related business rules:
  - Apply, cancel, approve, reject, override.
  - Build employee dashboard (requests + balances).
  - Read lists for employee, manager, HR.
- Controllers are kept thin: they resolve the current user, call service methods, and handle validation or redirects.

### Security and roles
- ASP.NET Core Identity (`ApplicationUser` + `IdentityRole`) with EF Core store.
- Role‑protected controllers:
  - `EmployeeLeaveController` → `[Authorize(Roles = "Employee")]`
  - `ManagerLeaveController` → `[Authorize(Roles = "Manager")]`
  - `HrLeaveController` → `[Authorize(Roles = "HR")]`
  - `LeaveApiController.GetAll` → `[Authorize(Roles = "HR")]`
- Cookie authentication is configured so:
  - Unauthenticated → `/Account/Login`
  - Access denied → `/Account/AccessDenied`

### Logging and exception handling
- Serilog configured in `Program.cs`:
  - Sinks: console + SQL Server (`Logs` table, auto‑created).
  - Minimum level: `Information` with higher threshold for framework logs.
- `ExceptionLoggingMiddleware` wraps the pipeline and logs any unhandled exception with request path, before re‑throwing to the standard error handler.
- Key controllers use `try/catch` with Serilog:
  - Logs errors at `Error` level.
  - Logs known business validation failures at `Warning` (e.g., invalid date ranges on apply).

### Caching (basic)
- `AddMemoryCache` registered in DI.
- `LeaveService` uses `IMemoryCache` to cache:
  - Employee’s leave list.
  - Manager’s pending list.
  - HR’s “all leave” list.
- Cache entries are invalidated when any change occurs (apply, cancel, approve, reject, override) to keep lists consistent.

### UI / UX
- Shared layout with a modern gradient navbar.
- Login page styled as a centered card with gradient background.
- Employee dashboard:
  - Cards showing leave balances per type (Annual, Sick, Casual).
  - Table listing all requests with status and actions.
- Manager dashboard:
  - Table of pending approvals with Approve/Reject actions.
- HR dashboard:
  - Summary cards (total, pending, approved, rejected).
  - Table of all requests with override controls.

---

## Assumptions and Trade‑offs

### Leave policy simplifications
- Leave types are simple strings (`Annual`, `Sick`, `Casual`).
- Per‑employee allowances are hard‑coded in `LeaveService`:
  - Annual: 20 days, Sick: 10 days, Casual: 7 days.
- Balances are computed from approved leave records at runtime, not persisted separately.
- No half‑day or hourly leave; days are counted as whole days (inclusive of start and end dates).

### Manager / employee relationships
- Managers can currently see and act on **all** pending leaves, not only direct reports.
- The data model does not capture a manager–employee hierarchy; this keeps the demo simple.
- To enforce real reporting lines, you’d need additional relationships and filters in `GetPendingLeavesForManagerAsync`.

### Email notifications
- Real email sending is not implemented.
- `IEmailNotificationService` has a `LogEmailNotificationService` implementation that logs “emails” to Serilog for:
  - Apply, cancel, approve, reject, override.
- This keeps the sample self‑contained; plugging in a real SMTP or SendGrid implementation would be straightforward.

### Concurrency and CancellationTokens
- For simplicity, the code does **not** use `CancellationToken` parameters anymore.
- EF Core’s default optimistic concurrency (no explicit `RowVersion`) is relied upon:
  - Combined with business checks on current status, this is sufficient for a demo.
- In a high‑load or production system, you may want:
  - A `RowVersion` / concurrency token on `LeaveRequest`.
  - Propagate `CancellationToken` from HTTP to EF calls for graceful shutdown and timeouts.

### Data model and persistence
- A single SQL Server database holds Identity tables and leave tables.
- Soft delete is supported via `LeaveRequest.IsDeleted` but not heavily used in the UI yet.
- No background jobs or queues; all workflows are synchronous HTTP requests.

---

## Concurrency Handling Explanation

### Request / transaction boundaries
- Each HTTP request resolves:
  - A scoped `ApplicationDbContext` through `AddDbContext`.
  - A scoped `UnitOfWork` and `LeaveService`.
- All write operations in a request use that single `DbContext`:
  - Multiple repository operations.
  - Single `SaveChangesAsync()` call at the end of the service method.
- This is the standard “one unit of work per web request” pattern.

### Business‑level guards against invalid transitions
- Before changing status in `LeaveService`, the current status is checked:
  - Employee cancel:
    - Only if `leave.EmployeeId == employeeId` **and** `Status == Pending`.
  - Manager approve/reject:
    - Only if `Status == Pending`.
  - HR override:
    - Only if `newStatus` is one of `Pending/Approved/Rejected`.
    - Only if `newStatus != previousStatus`.
- These checks ensure that if two users act on the same leave at the same time:
  - The **first** valid operation wins.
  - The second operation will see the updated state and return `false` (no change), avoiding invalid transitions like “approve an already rejected leave”.

### EF Core and optimistic concurrency
- There is no explicit `RowVersion` column configured, so EF’s concurrency handling is:
  - Last write wins at the database level.
  - Business rules above prevent semantically invalid writes.
- If stricter concurrency is needed:
  - Add a `byte[] RowVersion` property with `[Timestamp]` or fluent configuration in `LeaveRequest`.
  - Handle `DbUpdateConcurrencyException` in the service layer to inform users when their change conflicts with a concurrent update.

### Caching and consistency
- In‑memory caches are always invalidated **after** a successful write:
  - Employee’s cache key.
  - Manager pending cache key.
  - HR all‑leaves cache key.
- Next read repopulates from the database, so:
  - There is a small window where other requests may see slightly stale data until the write completes.
  - Once the write transaction and cache invalidation are done, subsequent requests see the updated state.

This level of concurrency handling is intentionally “basic but safe enough” for a sample app. It demonstrates good patterns (per‑request unit of work, status checks, cache invalidation) without adding heavy infrastructure that would distract from the core leave workflow. 

---

## Database Migrations and Sample Data

### EF Core migrations

The project already includes an initial EF Core migration in the `Migrations` folder (e.g. `20251227165411_InitialCreate`).

To apply migrations and create/update the database schema:

```bash
cd EnterpriseLeaveManagement
dotnet ef database update
```

To generate a SQL script from the migrations (for deployment or review):

```bash
cd EnterpriseLeaveManagement
dotnet ef migrations script -o Migrations/InitialCreate.sql
```

This produces a `.sql` file you can run manually against SQL Server if needed.

### Sample users and roles

Sample users and their roles are seeded in `Data/IdentitySeeder.cs` and executed at startup via:

```csharp
await IdentitySeeder.SeedRolesAsync(app.Services);
```

Seeded roles:
- `Employee`
- `Manager`
- `HR`

Seeded users (password `Password@123`):
- `employee1@company.com` → `Employee`
- `manager1@company.com` → `Manager`
- `hr1@company.com` → `HR`

If these users already exist, the seeder ensures they are in the correct roles. You can modify `IdentitySeeder` if you need different sample data. 


