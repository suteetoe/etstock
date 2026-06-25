# Backend Contract 1 — Company Repository

## Interface

```csharp
namespace ETStock.Data.Repositories;

public interface ICompanyRepository
{
    Task<Company?> GetAsync(CancellationToken ct = default);
    Task UpsertAsync(Company company, CancellationToken ct = default);
}
```

`Company` is defined in the `ETStock.Models` namespace.

## Frontend DI resolution

```csharp
using ETStock.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

var repository = Program.Services!
    .CreateScope()
    .ServiceProvider
    .GetRequiredService<ICompanyRepository>();
```

Equivalent single-line expression:

```csharp
Program.Services!.CreateScope().ServiceProvider.GetRequiredService<ICompanyRepository>()
```

`ICompanyRepository` is registered with scoped lifetime.

## Company model fields

| Field | Type | Default |
|---|---|---|
| `Id` | `int` | `0` |
| `Name` | `string` | `string.Empty` |
| `TaxId` | `string` | `string.Empty` |
| `Address` | `string` | `string.Empty` |
| `BranchName` | `string` | `string.Empty` |
| `BranchCode` | `string` | `"00000"` |
| `InvoicePrefix` | `string` | `string.Empty` |
| `VatRate` | `decimal` | `0.07m` |

## Verification status

- Build: passed — 0 warnings, 0 errors
- Tests: passed — 8 total, 8 passed, 0 failed, including all 3 `CompanyRepositoryTests`
