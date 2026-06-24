# Phase 2 Frontend Notes — Product and Monthly Stock

## Screen

- The Product/Stock tab contains an add-product form for code, name, unit, cost
  price, and sell price.
- The period selector defaults to the current year/month and reloads all products
  with stock for the selected period.
- The stock table shows code, name, unit, opening quantity, cost price, purchases,
  full-invoice sales, POS sales, and closing quantity.
- Opening, purchase, full-invoice sale, and POS sale quantities are editable in
  memory. Each row has its own save action.

## MVVM bindings

- `ProductViewModel.AddProductCommand` validates required text and non-negative
  prices before calling `IProductRepository.AddAsync`.
- Duplicate product codes always display `รหัสสินค้านี้มีอยู่แล้ว`.
- `ProductViewModel.LoadCommand` calls
  `GetAllWithMonthlyStockAsync(year, month)`.
- `ProductStockRowViewModel` raises `ClosingQty` when any editable quantity
  changes. The calculation is `opening + buy - sellFull - sellPos`.
- `ProductStockRowViewModel.SaveCommand` calls
  `UpsertMonthlyStockAsync` through the parent view model.
- Both the view and row data template use `x:DataType` for compiled bindings.
- The parameterless design-time constructor does not access DI or the database.

## QA focus

- Required product fields reject blank or whitespace-only input.
- Cost and sell prices reject invalid or negative values.
- Exact duplicate codes produce the deterministic duplicate message.
- Changing any stock quantity updates closing immediately, including negative
  closing values.
- Switching year/month and loading displays only that period's stock values.
- Saving a new period inserts stock; saving again updates the same row.
- Repository failures leave the UI usable and display an error message.
