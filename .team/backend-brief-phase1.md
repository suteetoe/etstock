# Backend Brief — Phase 1 Company Repository

## Context

You are a Senior .NET Backend Engineer on the ETStock project.
Working directory: D:\Source\ETStock
Current branch: feature/phase-1-company (already checked out — do NOT create or switch branches)

## Stack
- .NET 10, EF Core 10.0.7 + Npgsql 10.0.0, PostgreSQL
- CommunityToolkit.Mvvm 8.2.1 (for MVVM — but that's the frontend's domain)
- xUnit 2.9.3 + Microsoft.EntityFrameworkCore.InMemory 10.0.0 (test project)

## Existing code to read first

1. ETStock/Models/Company.cs — entity already defined
2. ETStock/Data/AppDbContext.cs — has DbSet<Company> Companies
3. ETStock/Data/Repositories/ — does NOT exist yet, create it
4. ETStock.Tests/Phase0SmokeTests.cs — see how existing tests are structured
5. ETStock.Tests/ETStock.Tests.csproj — check package references
6. ETStock/Program.cs — see how DI services are registered (AddDbContext)
7. ETStock/ETStock.csproj — check project references

## Task

Create the Company repository layer:

### 1. ETStock/Data/Repositories/ICompanyRepository.cs

```csharp
namespace ETStock.Data.Repositories;

public interface ICompanyRepository
{
    Task<Company?> GetAsync(CancellationToken ct = default);
    Task UpsertAsync(Company company, CancellationToken ct = default);
}
```

Note: Company model is in ETStock.Models namespace. Add appropriate using.

### 2. ETStock/Data/Repositories/CompanyRepository.cs

Implement ICompanyRepository using EF Core async:
- GetAsync: return first Company record or null (there is only ever one company record)
  Use `await _db.Companies.FirstOrDefaultAsync(ct)` 
- UpsertAsync: 
  - Get existing record
  - If null: _db.Companies.Add(company); await _db.SaveChangesAsync(ct);
  - If exists: copy all fields from company parameter to existing entity, then SaveChangesAsync
    Fields to copy: Name, TaxId, Address, BranchName, BranchCode, InvoicePrefix, VatRate
- Constructor: `public CompanyRepository(AppDbContext db) { _db = db; }`

### 3. Register in ETStock/Program.cs

Add inside `ConfigureServices`:
```csharp
services.AddScoped<ICompanyRepository, CompanyRepository>();
```
Add necessary using statement.

### 4. ETStock.Tests/CompanyRepositoryTests.cs

Write xUnit tests using InMemory DB (same pattern as Phase0SmokeTests.cs):

```csharp
public class CompanyRepositoryTests
{
    private static AppDbContext CreateDb() => new AppDbContext(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact] public async Task GetAsync_ReturnsNull_WhenEmpty() { ... }
    [Fact] public async Task UpsertAsync_Inserts_WhenNoRecord() { ... }
    [Fact] public async Task UpsertAsync_Updates_WhenRecordExists() { ... }
}
```

Use `Guid.NewGuid().ToString()` as DB name so each test gets a fresh database.

### 5. Build and test

Run: dotnet build ETStock.slnx
Run: dotnet test ETStock.slnx

Both must pass with 0 errors before finishing.

### 6. Write contract file

Write D:\Source\ETStock\.team\backend-contract-1.md with:
- Full interface signatures
- How Frontend should resolve ICompanyRepository from DI:
  `Program.Services!.CreateScope().ServiceProvider.GetRequiredService<ICompanyRepository>()`
- Company model fields for Frontend reference
- Build and test status

### 7. Commit your work

```
cd D:\Source\ETStock
git add ETStock/Data/Repositories/ ETStock/Program.cs ETStock.Tests/CompanyRepositoryTests.cs .team/backend-contract-1.md
git commit -m "feat(be): add ICompanyRepository + CompanyRepository [phase 1]"
```

Do NOT push. Do NOT open a PR. The Team Lead handles git push and PR.

## Acceptance criteria
- 0 build errors
- 3 new tests pass
- ICompanyRepository interface defined with GetAsync + UpsertAsync
- CompanyRepository registered as Scoped in DI
- .team/backend-contract-1.md written
- Changes committed on current branch
