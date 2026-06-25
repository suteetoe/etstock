# Frontend Agent Brief - Phase 3 Monthly Transaction UI

## Scope

Allowed files:

- `ETStock/ViewModels/**`
- `ETStock/Views/**`
- `ETStock/Program.cs` only if needed for DI wiring
- targeted tests only if needed to support view model behavior
- `.team/frontend-notes-phase3.md`

Do not edit backend repository implementation except for compile fixes strictly required by the existing contract.

## Backend Contract Available

Use `IMonthlyStockRepository`:

- `GetPeriodAsync(int year, int month, CancellationToken ct = default)`
- `SaveAsync(MonthlyStockInput stock, CancellationToken ct = default)`
- `CarryForwardAsync(int year, int month, CancellationToken ct = default)`

Important DTOs are in `ETStock/Data/Repositories/MonthlyStockRepository.cs`.

## Required UI / VM Behavior

- Replace the placeholder product/stock screen with a usable monthly transaction screen.
- Provide year and month controls.
- Load rows for all products in the selected period.
- Show product code, name, unit, cost, sell price.
- Allow editing opening, buy, full-invoice sale, and POS sale quantities.
- Show closing quantity as a calculated display value.
- Provide commands for load/change period, carry forward, and save.
- Keep MVVM pattern: no business logic in code-behind.
- Preserve compiled binding with correct `x:DataType`.

## Output

- Commit product code changes only.
- Write concise implementation notes to `.team/frontend-notes-phase3.md`.
- Do not push or open PR.
