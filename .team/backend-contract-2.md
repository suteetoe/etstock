# Backend Contract 2 — Product and Monthly Stock

Namespace: `ETStock.Data.Repositories`

`IProductRepository` is registered as a scoped service and can be resolved through
`Program.Services` inside a dependency-injection scope.

## Public interface

```csharp
public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductWithStock>> GetAllWithMonthlyStockAsync(
        int year, int month, CancellationToken ct = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<ProductWriteResult> AddAsync(
        Product product, CancellationToken ct = default);
    Task<ProductWriteResult> UpdateAsync(
        Product product, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<MonthlyStockSnapshot> UpsertMonthlyStockAsync(
        MonthlyStockInput stock, CancellationToken ct = default);
}
```

All reads are no-tracking. All database operations are asynchronous and accept a
`CancellationToken`.

## DTOs and result types

```csharp
public enum ProductWriteResult
{
    Success,
    DuplicateCode,
    NotFound
}

public sealed record ProductWithStock(
    int Id,
    string Code,
    string Name,
    string Unit,
    decimal CostPrice,
    decimal SellPrice,
    MonthlyStockSnapshot? MonthlyStock);

public sealed record MonthlyStockInput(
    int ProductId,
    int Year,
    int Month,
    decimal OpeningQty,
    decimal BuyQty,
    decimal SellFullQty,
    decimal SellPosQty);

public sealed record MonthlyStockSnapshot(
    int Id,
    int ProductId,
    int Year,
    int Month,
    decimal OpeningQty,
    decimal BuyQty,
    decimal SellFullQty,
    decimal SellPosQty)
{
    public decimal ClosingQty =>
        OpeningQty + BuyQty - SellFullQty - SellPosQty;
}
```

`ProductWithStock.MonthlyStock` is `null` when the product has no stock row for
the requested year and month.

## Duplicate-code behavior

`Product.Code` remains protected by the existing unique database index.

- `AddAsync` returns `ProductWriteResult.DuplicateCode` when the exact code
  already exists.
- `UpdateAsync` returns `DuplicateCode` when another product owns the exact code.
- A PostgreSQL unique-index race is also converted to `DuplicateCode`.
- No provider-specific exception message needs to be displayed by the frontend.

Code comparison follows the database's existing equality/collation behavior; no
case-folding or schema migration was introduced.

## Monthly stock behavior

`GetAllWithMonthlyStockAsync(year, month)` returns every product and at most one
matching monthly-stock snapshot. Results are ordered by product code.

`UpsertMonthlyStockAsync` inserts the row when `(ProductId, Year, Month)` is new,
or updates all four quantity fields when it already exists. It returns the saved
snapshot. A missing product id throws `ArgumentException`. Year must be positive
and month must be 1 through 12.

Closing stock is computed in application code:

```text
closing = opening + buy - sellFull - sellPos
```

Negative closing values are allowed and returned unchanged.

## Delete and update results

- `UpdateAsync` returns `NotFound` when the product id does not exist.
- `DeleteAsync` returns `false` when the product id does not exist; otherwise it
  deletes and returns `true`.

## Schema status

No migration is required. The Phase 0 unique indexes remain compatible:

- `Products.Code`
- `MonthlyStocks(ProductId, Year, Month)`

## Verification status

- Build: passed — 0 warnings, 0 errors.
- Tests: passed — 26 total, 26 passed, 0 failed.
- Verification used isolated temporary build output because the sandbox denied
  writes to the repository's existing `obj` cache and Avalonia telemetry log.
