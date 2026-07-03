# Backend Contract - Export Excel

Phase: export-excel

## ProductViewModel API for frontend

- Command: `ExportExcelCommand`
- Event: `ExportExcelRequested`
- Completion method: `CompleteExportExcel(string? path)`
- Default filename expected from FE SaveFilePicker: `Stock_{year}_{month:00}.xlsx`

## Event pattern

`ProductViewModel` publishes `ExportExcelRequested` when `ExportExcelCommand` needs a file path.
The View should subscribe to the event, open a SaveFilePicker, then call `CompleteExportExcel(path)`.

- Pass a local `.xlsx` path to continue export.
- Pass `null` or an empty string when the user cancels.

## Backend behavior

- If monthly stock has unsaved changes, `ExportExcelCommand` saves rows before export and reloads the period.
- `IExcelExportService.WriteStock(rows, year, month, filePath)` writes worksheet `Stock_{year}_{month:00}`.
- Excel export columns are:
  1. `ลำดับ` -> `LineNumber`
  2. `ชื่อสินค้า` -> `Name`
  3. `ยอดยกมา` -> `OpeningQty`
  4. `ซื้อเข้า` -> `BuyQty`
  5. `ขายออก` -> `SalesQty`
  6. `บิลเต็ม` -> `SellFullQty`
  7. `คงเหลือ` -> `ClosingQty`
  8. `ราคาขาย` -> `SellPrice`
  9. `จำนวนเงิน` -> `SalesAmount`
  10. `ต้นทุนสินค้า` -> `CostPrice`
  11. `ต้นทุนรวม` -> `TotalCost`
  12. `มูลค่าสินค้าคงเหลือ` -> `ClosingValue`

## New computed row property

`MonthlyStockRowViewModel.TotalCost` returns `SalesQty * CostPrice`.
