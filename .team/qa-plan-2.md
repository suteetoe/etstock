# QA Plan — Phase 2 Product & Stock

## Scope

- Automated xUnit coverage under `ETStock.Tests`.
- Repository tests use EF Core InMemory; view-model tests use an
  `IProductRepository` fake.
- PostgreSQL is not required.
- Production code is read-only for this QA task.

## Acceptance-Criteria Mapping

| Acceptance criterion | Automated test |
|---|---|
| Closing formula returns positive, zero, and negative values | `ProductRepositoryTests.ClosingQty_ComputesPositiveZeroAndNegativeValues` |
| Duplicate product code returns deterministic `DuplicateCode` from `AddAsync` | `ProductRepositoryTests.AddAsync_ReturnsDuplicateCode_WhenCodeAlreadyExists` |
| Product view model shows the deterministic duplicate-code message | `ProductViewModelTests.AddProductAsync_ShowsDeterministicMessage_WhenCodeIsDuplicate` |
| Product view model rejects blank required fields | `ProductViewModelTests.AddProductAsync_RejectsBlankRequiredFields` |
| Product view model rejects negative cost or sell prices | `ProductViewModelTests.AddProductAsync_RejectsNegativePrices` |
| Row closing quantity updates live after each editable quantity changes | `ProductViewModelTests.ClosingQty_UpdatesLive_WhenAnyQuantityChanges` |
| Monthly stock upsert inserts, then updates the same product/year/month row | `ProductRepositoryTests.UpsertMonthlyStockAsync_InsertsThenUpdatesSamePeriod` |

## Execution

1. Run `dotnet build`.
2. Run `dotnet test`.
3. Record build/test results, issues, risks, and the final PASS/FAIL verdict in
   `.team/qa-report-2.md`.

## Pass Criteria

- `dotnet test` passes.
- Every required acceptance criterion is covered by an automated test.
- No production files are edited.
