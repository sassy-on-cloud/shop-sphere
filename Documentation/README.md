# ShopSphere Documentation and Local Setup

This folder contains the university project report, its Mermaid diagram sources, and the UI screenshots captured from a local Development run. The report describes the source code as inspected on 16 November 2026. Screenshots use seeded sample data and temporary test accounts; no credentials are included.

## Project requirements

- .NET 10 SDK
- Microsoft SQL Server reachable from the application machine
- Entity Framework Core CLI compatible with EF Core 10.0.11
- A modern browser

The project targets `net10.0` and uses ASP.NET Core MVC, ASP.NET Core Identity, Entity Framework Core SQL Server, Razor, HTML, CSS, JavaScript, and Bootstrap 5. See [`ShopSphere.csproj`](../ShopSphere.csproj) for package versions.

## Configure a local SQL Server connection

From the repository root, initialize User Secrets once and set a connection string for your own SQL Server instance. Adjust the server and database names to match your local setup:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:ShopSphere" "Server=localhost;Database=ShopSphereDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

For SQL Server Express, the server portion is often `localhost\SQLEXPRESS`; for LocalDB, it is commonly `(localdb)\MSSQLLocalDB`. Use the authentication method configured on your SQL Server.

The application also supports an optional bootstrap administrator. Set these values through User Secrets or environment variables, not committed JSON:

```powershell
dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.test"
dotnet user-secrets set "BootstrapAdmin:Password" "<unique password of at least 12 characters>"
dotnet user-secrets set "BootstrapAdmin:FullName" "Shop Administrator"
```

Choose a private password of at least 12 characters that also satisfies the configured Identity policy (uppercase, lowercase, a digit and a non-alphanumeric character). The bootstrap process creates the configured Admin account only when that email does not already identify a user. It does not promote an existing account. The application initializes the `Customer` and `Admin` roles at startup. Never use the example email or a sample password as production credentials.

## Restore, migrate, and run

From the repository root:

```powershell
dotnet restore
dotnet build
dotnet ef database update
dotnet run --launch-profile https
```

If `dotnet ef` is not installed, install the matching command-line tool:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.11
```

The migrations are applied explicitly; startup does **not** create or migrate the database. Create/upgrade the schema with `dotnet ef database update` before starting the application. Four migrations are present:

1. `20261115151547_InitialCreate`
2. `20261116075233_AddIdentity`
3. `20261116140930_AddShoppingCart`
4. `20261116143024_AddOrders`

Development data seeding runs only when `ASPNETCORE_ENVIRONMENT` is `Development`. It creates six sample categories and 18 sample products, skipping records with names that already exist. Run the application in Development only against a database where this seed data is appropriate. The sample catalog is not seeded in other environments.

The launch profiles define `http://localhost:5109` and `https://localhost:7052` (with the HTTPS profile also listening on the HTTP URL). Trust the local ASP.NET Core development certificate if your machine prompts for it:

```powershell
dotnet dev-certs https --trust
```

## Report and supporting files

- [`ShopSphere_Project_Report.md`](ShopSphere_Project_Report.md) — complete numbered report
- [`diagrams/ERD.mmd`](diagrams/ERD.mmd) — entity-relationship diagram source
- [`diagrams/Use_Case_Diagram.mmd`](diagrams/Use_Case_Diagram.mmd) — use case diagram source
- [`diagrams/DFD.mmd`](diagrams/DFD.mmd) — Level 0 and Level 1 data flow diagram source
- [`ShopSphere_Project_Report.docx`](ShopSphere_Project_Report.docx) — formatted Word version
- [`screenshots/`](screenshots/) — local screenshots used by the report

The current application supports Cash on Delivery only. It does not integrate an online payment gateway or a delivery carrier.

## Run automated tests

The separate `ShopSphere.Tests` project targets .NET 10 and uses xUnit with EF Core SQLite. Tests create a fresh in-memory SQLite database per test and do not read or modify the application's SQL Server database or user secrets.

From the repository root, run:

```powershell
dotnet test .\ShopSphere.Tests\ShopSphere.Tests.csproj
```

The tests cover cart pricing and stock limits, guest-cart merging, order totals and inventory updates, insufficient-stock rejection, repeated checkout submission, cancellation stock restoration, role authorization metadata, and registration validation.
