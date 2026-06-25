# Backend Contract — Phase 0 Foundation

## Packages Added (ETStock/ETStock.csproj)

| Package | Version | Notes |
|---|---|---|
| `Microsoft.EntityFrameworkCore` | 10.0.7 | Core ORM |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.7 | `PrivateAssets="All"` — design-time only |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.0 | PostgreSQL provider |
| `Microsoft.Extensions.Hosting` | 10.0.0 | Generic host + DI |

`<ImplicitUsings>enable</ImplicitUsings>` was also added to the main csproj — it was absent from the original template and required for the brief's model code (which uses `ICollection<>` and `DateTime` without explicit `using` statements).

## Entity Models

All under namespace `ETStock.Models`:

| Class | File Path |
|---|---|
| `Company` | `ETStock/Models/Company.cs` |
| `Product` | `ETStock/Models/Product.cs` |
| `MonthlyStock` | `ETStock/Models/MonthlyStock.cs` |
| `AbbrInvoice` | `ETStock/Models/AbbrInvoice.cs` |
| `AbbrInvoiceItem` | `ETStock/Models/AbbrInvoiceItem.cs` |

## AppDbContext

- **Location:** `ETStock/Data/AppDbContext.cs`
- **Namespace:** `ETStock.Data`
- **Class:** `AppDbContext` (primary constructor, inherits `DbContext`)
- **DbSets:** `Companies`, `Products`, `MonthlyStocks`, `AbbrInvoices`, `AbbrInvoiceItems`
- **Indexes:** `Product.Code` (unique), `MonthlyStock(ProductId, Year, Month)` (unique)
- **Precision:** All `decimal` financial columns configured with `HasPrecision(18, 4)`

## Connection String

- **Key:** `ConnectionStrings:DefaultConnection`
- **Config file:** `ETStock/appsettings.json` (copied to output as `PreserveNewest`)
- **Default value:** `Host=localhost;Port=5432;Database=etstock;Username=etstock;Password=etstock`

## Accessing DbContext from DI

```csharp
// From anywhere in the app after startup:
using var scope = Program.Services!.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
```

`Program.Services` is a `public static IServiceProvider?` set during `Main()` after the generic host is built. The `AppDbContext` is registered as a scoped service via `AddDbContext<AppDbContext>()`.

## Test Project

- **Location:** `ETStock.Tests/ETStock.Tests.csproj`
- **Framework:** `net10.0`
- **Packages:** `xunit 2.9.3`, `xunit.runner.visualstudio 2.8.2`, `Microsoft.NET.Test.Sdk 17.12.0`
- **References:** `ETStock/ETStock.csproj`
- **Added to:** `ETStock.slnx`

## Deviations from Brief

1. **`ImplicitUsings` enabled** — The brief's model code uses `ICollection<>` and `DateTime` without `using` directives. The original csproj lacked `<ImplicitUsings>enable</ImplicitUsings>`, which caused build errors CS0246. Adding it is the standard .NET 6+ approach and does not affect existing code.
2. **`<Folder Include="Models\" />` removed** — The original csproj had a placeholder folder item for `Models\` that became redundant once the real `.cs` files were added. Removed to keep the project file clean.

## Build Status

`dotnet build ETStock.slnx` — **0 errors, 0 warnings** (both projects compile successfully).
