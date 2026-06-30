# Backend Contract - Fix InvoiceDate Column Type

Namespace: `ETStock.Data`

## Schema change

`AbbrInvoice.InvoiceDate` is mapped as a PostgreSQL local timestamp:

```csharp
modelBuilder.Entity<AbbrInvoice>()
    .Property(i => i.InvoiceDate)
    .HasColumnType("timestamp without time zone");
```

This matches the domain meaning of the field: invoice date only, with no
timezone semantics.

## Migration

Migration:
`ETStock/Data/Migrations/20260630022818_ChangeInvoiceDateColumnType.cs`

`Up` alters `AbbrInvoices.InvoiceDate` from `timestamp with time zone` to
`timestamp without time zone`.

`Down` restores `AbbrInvoices.InvoiceDate` to `timestamp with time zone`.

`AppDbContextModelSnapshot` is updated to `timestamp without time zone`.

## Regression coverage

`AbbrInvoiceRepositoryTests.SaveAsync_UnspecifiedInvoiceDate_DoesNotThrow`
confirms an `AbbrInvoice` with `InvoiceDate` created by
`new DateTime(year, month, day)` keeps `DateTimeKind.Unspecified` and saves
through the repository using the EF InMemory provider.

The test is a structural regression guard for entity/repository behavior.
Npgsql is the provider that enforces `DateTimeKind` for `timestamptz`.

## Verification status

- Build: passed - `dotnet build`, 0 warnings, 0 errors.
- Focused tests: passed - `dotnet test --no-build --filter "FullyQualifiedName~AbbrInvoiceRepositoryTests|FullyQualifiedName~AbbrInvoiceRepositoryDeleteByPeriodTests"`, 11 passed.
- Full tests: not green - `dotnet test --no-build` ran 75 tests with 74 passed and 1 failed:
  `ETStock.Tests.MonthlyStockRepositoryTests.CarryForwardAsync_UpdatesOpeningWithoutReplacingCurrentTransactions`
  expected `BuyQty` 7 but got 0. The same test fails when run alone and is outside
  the InvoiceDate mapping scope.
- Initial full `dotnet test` with rebuild also hit an `ETStock.pdb` file lock from
  a running `.NET Host` process before tests executed.
