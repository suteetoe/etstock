# QA Report — Phase 0 Foundation

**Date:** 2026-06-24
**Branch:** feature/phase-0-foundation
**Verdict:** PASS

---

## Test Results

| Test | Result |
|---|---|
| `DbContext_ContainsAllRequiredEntities` | PASS |
| `Company_HasRequiredProperties` | PASS |
| `Product_HasRequiredProperties` | PASS |
| `MonthlyStock_ClosingFormula_IsCorrect` | PASS |
| `Migration_InitialFilesExist` | PASS |

**Total: 5 passed, 0 failed, 0 skipped** — Duration: ~1 s

Command: `dotnet test ETStock.slnx`

---

## Migration Status

**Files generated in `ETStock/Data/Migrations/`:**
- `20260624035619_Initial.cs`
- `20260624035619_Initial.Designer.cs`
- `AppDbContextModelSnapshot.cs`

**Tables in idempotent SQL script (`dotnet ef migrations script --idempotent`):**

| Table | Present |
|---|---|
| `AbbrInvoices` | YES |
| `Companies` | YES |
| `Products` | YES |
| `AbbrInvoiceItems` | YES |
| `MonthlyStocks` | YES |

All 5 tables confirmed. Indexes included:
- `IX_Products_Code` (UNIQUE)
- `IX_MonthlyStocks_ProductId_Year_Month` (UNIQUE)
- `IX_AbbrInvoiceItems_AbbrInvoiceId`
- `IX_AbbrInvoiceItems_ProductId`

Decimal precision `numeric(18,4)` applied to all financial columns.

---

## Files Created

| File | Purpose |
|---|---|
| `ETStock/Data/DesignTimeDbContextFactory.cs` | IDesignTimeDbContextFactory for `dotnet ef` CLI |
| `ETStock/Data/Migrations/20260624035619_Initial.cs` | Initial EF Core migration |
| `ETStock/Data/Migrations/20260624035619_Initial.Designer.cs` | Migration designer snapshot |
| `ETStock/Data/Migrations/AppDbContextModelSnapshot.cs` | EF model snapshot |
| `ETStock.Tests/Phase0SmokeTests.cs` | 5 Phase 0 smoke tests |

**Modified:**
- `ETStock.Tests/ETStock.Tests.csproj` — added `ImplicitUsings`, added `Microsoft.EntityFrameworkCore.InMemory 10.0.0`

---

## Issues Found and Fixed

1. **Missing `IDesignTimeDbContextFactory`** — `dotnet ef` needs a design-time factory when the main project is an Avalonia WinExe (no host builder accessible at design time). Created `DesignTimeDbContextFactory.cs` as specified in the brief. Migration then succeeded.

2. **Missing `ImplicitUsings` in test project** — The test project csproj lacked `<ImplicitUsings>enable</ImplicitUsings>`. Without it, `Path`, `AppContext`, and `DirectoryInfo` in Test 5 were unresolved (CS0103/CS0246). Fixed by adding the property.

3. **EF Core InMemory version pinned to 10.0.0** — Brief specified 10.0.7, but the main project's Npgsql 10.0.0 dependency brings in `Microsoft.EntityFrameworkCore.Relational` 10.0.0 as a transitive dependency, causing an MSB3277 conflict warning with InMemory 10.0.7. Pinned InMemory to 10.0.0 to match Npgsql's transitive dependency and eliminate the conflict. Tests compile and pass cleanly.

---

## Outstanding Issues

None. All Phase 0 DoD items are met:
- `dotnet build ETStock.slnx` — 0 errors
- `dotnet test ETStock.slnx` — 5/5 tests pass
- EF Core migration files exist in `ETStock/Data/Migrations/`
- Migration SQL includes all 5 tables
