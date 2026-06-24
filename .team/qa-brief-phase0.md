# QA Brief — Phase 0 Foundation

**Branch:** feature/phase-0-foundation (already exists; just edit files)
**Commit convention:** `test(qa): <description> [phase 0]`
**DO NOT run any git commands.**

## Context
Read backend contract: D:\Source\ETStock\.team\backend-contract-phase0.md
Read frontend notes: D:\Source\ETStock\.team\frontend-notes-phase0.md

Phase 0 DoD:
- `dotnet build` ผ่าน
- แอปเปิดได้
- migration รันกับ PostgreSQL ได้

## Objective

1. Create EF Core initial migration (design-time, no DB needed)
2. Write smoke tests in `ETStock.Tests`
3. Verify all Phase 0 DoD items pass

## Tasks

### 1. Create EF Core Initial Migration

Run this command from `D:\Source\ETStock`:
```
dotnet ef migrations add Initial --project ETStock --startup-project ETStock --output-dir Data/Migrations
```

If this fails because EF cannot find an `IDesignTimeDbContextFactory<AppDbContext>`,
create one first at `ETStock/Data/DesignTimeDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ETStock.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=etstock;Username=etstock;Password=etstock")
            .Options;
        return new AppDbContext(options);
    }
}
```

After creating the factory, re-run the migration command.
The migration files should appear in `ETStock/Data/Migrations/`.

### 2. Write Smoke Tests in ETStock.Tests

Create `ETStock.Tests/Phase0SmokeTests.cs` with these test cases:

```csharp
using ETStock.Data;
using ETStock.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ETStock.Tests;

public class Phase0SmokeTests
{
    // Test 1: Verify all entity types are registered in DbContext
    [Fact]
    public void DbContext_ContainsAllRequiredEntities()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("smoke-test")
            .Options;

        using var ctx = new AppDbContext(options);

        Assert.NotNull(ctx.Companies);
        Assert.NotNull(ctx.Products);
        Assert.NotNull(ctx.MonthlyStocks);
        Assert.NotNull(ctx.AbbrInvoices);
        Assert.NotNull(ctx.AbbrInvoiceItems);
    }

    // Test 2: Verify entity model properties exist
    [Fact]
    public void Company_HasRequiredProperties()
    {
        var c = new Company
        {
            Name = "บริษัท ทดสอบ จำกัด",
            TaxId = "0105566001234",
            VatRate = 0.07m,
            InvoicePrefix = "AB"
        };
        Assert.Equal("บริษัท ทดสอบ จำกัด", c.Name);
        Assert.Equal(0.07m, c.VatRate);
    }

    // Test 3: Verify Product unique code model
    [Fact]
    public void Product_HasRequiredProperties()
    {
        var p = new Product { Code = "P001", Name = "สินค้าทดสอบ", Unit = "ชิ้น", SellPrice = 100m };
        Assert.Equal("P001", p.Code);
        Assert.Equal(100m, p.SellPrice);
    }

    // Test 4: MonthlyStock closing formula
    [Fact]
    public void MonthlyStock_ClosingFormula_IsCorrect()
    {
        var ms = new MonthlyStock
        {
            OpeningQty = 100,
            BuyQty = 50,
            SellFullQty = 30,
            SellPosQty = 20
        };
        // closing = opening + buy - sellFull - sellPos
        var closing = ms.OpeningQty + ms.BuyQty - ms.SellFullQty - ms.SellPosQty;
        Assert.Equal(100m, closing);
    }

    // Test 5: Migration files exist
    [Fact]
    public void Migration_InitialFilesExist()
    {
        var migrationDir = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "ETStock", "Data", "Migrations");

        var dir = new DirectoryInfo(migrationDir);
        Assert.True(dir.Exists, $"Migrations directory not found at: {dir.FullName}");

        var migrationFiles = dir.GetFiles("*_Initial.cs");
        Assert.True(migrationFiles.Length > 0, "No Initial migration file found in Data/Migrations/");
    }
}
```

**Important:** Test 1 uses InMemoryDatabase — you need to add this package to `ETStock.Tests.csproj`:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.7" />
```

### 3. Run Tests

After writing, run:
```
dotnet test ETStock.slnx 2>&1
```

All tests must pass (except possibly Test 5 if migrations weren't generated — fix the root cause, don't skip the test).

### 4. Check Migration SQL (optional verification)

After migration is created, run (no DB needed — just lists the SQL):
```
dotnet ef migrations script --idempotent --project ETStock --startup-project ETStock
```

Scan the output: verify tables `companies`, `products`, `monthly_stocks`, `abbr_invoices`, `abbr_invoice_items` are present.

## Definition of Done

- `dotnet build ETStock.slnx` — 0 errors
- `dotnet test ETStock.slnx` — all tests pass (green)
- EF Core migration files exist in `ETStock/Data/Migrations/`
- Migration SQL includes all 5 tables

## Output: write report to .team/qa-report-phase0.md

Write `.team/qa-report-phase0.md` with:
- **Verdict:** PASS or FAIL
- Test results (list each test: name + result)
- Migration status (files generated, table list from SQL)
- Any issues found and whether they were fixed
- Outstanding items that block DoD (if FAIL)
