# QA Brief — Phase 1 Company Repository + ViewModel Tests

## Context

You are a QA Engineer on the ETStock project.
Working directory: D:\Source\ETStock
Current branch: feature/phase-1-company (already checked out — do NOT create or switch branches)

## Stack
- xUnit 2.9.3, Microsoft.EntityFrameworkCore.InMemory 10.0.0
- ETStock.Tests/ETStock.Tests.csproj references ETStock/ETStock.csproj
- Pattern: see ETStock.Tests/Phase0SmokeTests.cs for how tests are structured

## Read these files first

1. ETStock/Data/Repositories/ICompanyRepository.cs — interface to test against
2. ETStock/Data/Repositories/CompanyRepository.cs — implementation
3. ETStock/ViewModels/CompanyViewModel.cs — ViewModel to test
4. ETStock.Tests/Phase0SmokeTests.cs — see test pattern
5. ETStock.Tests/ETStock.Tests.csproj — check packages

## Task

### 1. ETStock.Tests/CompanyRepositoryTests.cs

Write 3 xUnit tests for CompanyRepository using InMemory DB:

```csharp
using ETStock.Data;
using ETStock.Data.Repositories;
using ETStock.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ETStock.Tests;

public class CompanyRepositoryTests
{
    private static AppDbContext CreateDb() => new AppDbContext(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenEmpty()
    {
        using var db = CreateDb();
        var repo = new CompanyRepository(db);
        var result = await repo.GetAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertAsync_Inserts_WhenNoRecord()
    {
        using var db = CreateDb();
        var repo = new CompanyRepository(db);
        var company = new Company { Name = "Test Co", TaxId = "0000000000000", VatRate = 0.07m };
        await repo.UpsertAsync(company);
        var result = await repo.GetAsync();
        Assert.NotNull(result);
        Assert.Equal("Test Co", result.Name);
    }

    [Fact]
    public async Task UpsertAsync_Updates_WhenRecordExists()
    {
        using var db = CreateDb();
        var repo = new CompanyRepository(db);
        await repo.UpsertAsync(new Company { Name = "Old Name", TaxId = "1111111111111", VatRate = 0.07m });
        await repo.UpsertAsync(new Company { Name = "New Name", TaxId = "2222222222222", VatRate = 0.10m });
        var result = await repo.GetAsync();
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("2222222222222", result.TaxId);
        Assert.Equal(0.10m, result.VatRate);
    }
}
```

### 2. ETStock.Tests/CompanyViewModelTests.cs

Write 3 xUnit tests for CompanyViewModel.

Use a mock/fake repository. You can create a simple FakeCompanyRepository:

```csharp
using ETStock.Data.Repositories;
using ETStock.Models;
using ETStock.ViewModels;
using Xunit;

namespace ETStock.Tests;

// Simple in-memory fake for testing
internal class FakeCompanyRepository(Company? initial = null) : ICompanyRepository
{
    private Company? _stored = initial;
    public Task<Company?> GetAsync(CancellationToken ct = default) => Task.FromResult(_stored);
    public Task UpsertAsync(Company company, CancellationToken ct = default)
    {
        _stored = company;
        return Task.CompletedTask;
    }
}

public class CompanyViewModelTests
{
    [Fact]
    public async Task LoadAsync_PopulatesProperties_WhenCompanyExists()
    {
        var repo = new FakeCompanyRepository(new Company
        {
            Name = "บริษัท ทดสอบ",
            TaxId = "0105566001234",
            VatRate = 0.07m,
            InvoicePrefix = "AB"
        });
        var vm = new CompanyViewModel(repo);
        await vm.LoadCommand.ExecuteAsync(null);
        Assert.Equal("บริษัท ทดสอบ", vm.Name);
        Assert.Equal("0105566001234", vm.TaxId);
        Assert.Equal("AB", vm.InvoicePrefix);
    }

    [Fact]
    public async Task SaveAsync_CallsUpsert_WithCorrectValues()
    {
        var repo = new FakeCompanyRepository();
        var vm = new CompanyViewModel(repo)
        {
            Name = "บริษัท ทดสอบ",
            TaxId = "0105566001234",
            Address = "123 ถนนทดสอบ",
            VatRate = "7"
        };
        await vm.SaveCommand.ExecuteAsync(null);
        var saved = await repo.GetAsync();
        Assert.NotNull(saved);
        Assert.Equal("บริษัท ทดสอบ", saved.Name);
        Assert.Equal(0.07m, saved.VatRate);
    }

    [Fact]
    public async Task LoadAsync_SetsNoProperties_WhenNoCompanyRecord()
    {
        var repo = new FakeCompanyRepository(null);
        var vm = new CompanyViewModel(repo);
        await vm.LoadCommand.ExecuteAsync(null);
        // Properties stay at default values
        Assert.Equal(string.Empty, vm.Name);
        Assert.Equal(string.Empty, vm.TaxId);
    }
}
```

### 3. Build and test

Run: dotnet build ETStock.slnx
Run: dotnet test ETStock.slnx --verbosity normal

Record exact output (test count, pass/fail, duration).

### 4. Write QA report

Write D:\Source\ETStock\.team\qa-report-1.md with:
- Verdict: PASS or FAIL (PASS only if 0 build errors AND all tests pass)
- Test results table
- Any issues found

### 5. Commit

```
cd D:\Source\ETStock
git add ETStock.Tests/CompanyRepositoryTests.cs ETStock.Tests/CompanyViewModelTests.cs .team/qa-report-1.md
git commit -m "test(qa): add CompanyRepository + CompanyViewModel tests [phase 1]"
```

Do NOT push. Do NOT open a PR.

## Acceptance criteria
- 0 build errors
- 3 + 3 = 6 new tests all pass (total 11 tests: 5 Phase0 + 6 Phase1)
- Verdict = PASS written in qa-report-1.md
- Changes committed

## IMPORTANT
- You are QA only. Do NOT modify production code (ETStock/*.cs, ETStock/Views/*.axaml)
- If a test fails because production code has a bug, report FAIL with the specific error — do not fix it yourself
- If production code compiles but a test assertion fails, report FAIL
