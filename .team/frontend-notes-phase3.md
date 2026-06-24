# Frontend Notes - Phase 3

- Replaced the product stock placeholder with a monthly transaction grid.
- Added year/month fields and load, carry-forward, and save commands.
- Added editable opening, buy, full-invoice sale, and POS sale quantities with calculated closing quantity.
- Wired `ProductViewModel` to `IMonthlyStockRepository` from DI while preserving a design-time/default constructor.
- Verified with `dotnet test ETStock.slnx`: PASS.
