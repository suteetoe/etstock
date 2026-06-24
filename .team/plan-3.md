# Phase 3 Plan - Monthly Transaction

## Objective

Build the monthly transaction workflow for stock:

- select year/month
- load all products with the selected month's stock row
- edit opening, buy, full-invoice sale, and POS sale quantities
- save product/month rows
- carry previous month closing quantity into the selected month opening quantity

## Dependency Order

1. Backend: monthly stock repository contract and implementation.
2. Frontend: monthly transaction view model and UI using the backend contract.
3. QA: repository and view model regression coverage plus final verdict.

## Branch / PR Shape

- `backend/phase3-monthly-transaction`: backend repository and tests.
- `frontend/phase3-monthly-transaction-ui`: stacked on backend branch.
- `qa/phase3-monthly-transaction-tests`: stacked on frontend branch.

Do not push or open PRs until the Product Owner approves publishing.

## Acceptance Criteria

- `dotnet test ETStock.slnx` passes.
- Monthly stock closing quantity is calculated as opening + buy - full sale - POS sale.
- Carry forward copies previous month closing quantity into the current month opening quantity without deleting current-month transactions.
- Frontend exposes year/month controls, carry forward, save, and editable quantity columns.
- QA verdict is recorded in `.team/qa-report-phase3.md`.
