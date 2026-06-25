# Backend Brief — Phase 2: IProductRepository + ProductRepository

**Task ID:** P02-B
**Branch:** `backend/phase2-product-repo`
**Base:** `develop`

## Scope (edit ONLY these paths)

- `ETStock/Data/Repositories/IProductRepository.cs` (new)
- `ETStock/Data/Repositories/ProductRepository.cs` (new)
- `ETStock/Program.cs` (add DI registration)
- `ETStock.Tests/ProductRepositoryTests.cs` (new)

## Interface to implement

```csharp
// ETStock/Data/Repositories/IProductRepository.cs
using ETStock.Models;

namespace ETStock.Data.Repositories;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Product> AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
```

## Implementation rules

- EF Core async only (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`) — never `.Result`/`.Wait()`
- `GetAllAsync`: `OrderBy(p => p.Code)` before returning as `IReadOnlyList<Product>`
- `AddAsync`: add entity, save, return the entity (Id will be populated after save)
- `UpdateAsync`: load existing by Id, update all scalar fields (Code, Name, Unit, CostPrice, SellPrice), save; throw `KeyNotFoundException` if not found
- `DeleteAsync`: load existing by Id, remove, save; throw `KeyNotFoundException` if not found
- Constructor injection of `AppDbContext` (same pattern as CompanyRepository)

## DI registration (Program.cs)

Add immediately after the existing CompanyRepository line:
```csharp
services.AddScoped<IProductRepository, ProductRepository>();
```

## Tests (ETStock.Tests/ProductRepositoryTests.cs)

Use InMemory EF Core provider (already referenced — see CompanyRepositoryTests.cs for the pattern).

Required test methods:
- `GetAllAsync_ReturnsEmpty_WhenNoProducts`
- `AddAsync_AddsProduct_AndReturnsWithId`
- `GetByIdAsync_ReturnsProduct_WhenExists`
- `GetByIdAsync_ReturnsNull_WhenNotExists`
- `UpdateAsync_UpdatesAllFields`
- `UpdateAsync_Throws_WhenNotFound`
- `DeleteAsync_RemovesProduct`
- `DeleteAsync_Throws_WhenNotFound`

## Reference: existing patterns

- `ETStock/Data/Repositories/ICompanyRepository.cs`
- `ETStock/Data/Repositories/CompanyRepository.cs`
- `ETStock.Tests/CompanyRepositoryTests.cs`
- `ETStock/Models/Product.cs` (entity definition)
- `ETStock/Data/AppDbContext.cs` (DbContext with Products DbSet)

## Do NOT touch

- Any migration files
- Any ViewModel or View files
- Any other repository
