# Backend Contract — P6: InvoiceGeneratorService

## Namespace

```
ETStock.Services
```

## Records

```csharp
// Input: one POS stock line per product
public record PosStockLine(string ProductName, decimal SellPosQty, decimal SellPrice);

// Output: summary of what was generated
public record GenerateInvoicesResult(int InvoiceCount, decimal TotalAmount, decimal VatAmount);
```

## Interface: IInvoiceGeneratorService

```csharp
public interface IInvoiceGeneratorService
{
    /// Returns the number of AbbrInvoices already existing for the given period.
    /// Use this to prompt the user for confirmation before replacing.
    Task<int> GetExistingCountAsync(int taxYear, int taxMonth, CancellationToken ct = default);

    /// Generates random POS invoices (AbbrInvoice) from a list of POS stock lines.
    /// - Returns null if no stockLine has SellPosQty > 0.
    /// - If replaceExisting = true, all existing invoices for the period are deleted first.
    Task<GenerateInvoicesResult?> GenerateAsync(
        int taxYear,
        int taxMonth,
        IReadOnlyList<PosStockLine> stockLines,
        bool replaceExisting = false,
        CancellationToken ct = default);
}
```

## DI Registration (Program.cs)

```csharp
services.AddScoped<IInvoiceGeneratorService, InvoiceGeneratorService>();
```
Registered after `IInvoicePrintService`.

## IAbbrInvoiceRepository — new method

```csharp
Task DeleteByPeriodAsync(int taxYear, int taxMonth, CancellationToken ct = default);
```
Deletes all AbbrInvoice records (with cascade Items) for the given taxYear/taxMonth in a single `SaveChangesAsync` call.

## VAT Calculation Rules

- `SellPrice` in the database is **VAT-inclusive** (already includes 7% VAT).
- Per item:
  - `Amount    = Math.Round(qty * sellPrice / 1.07m, 2)`   // ex-VAT amount
  - `VatAmount = Math.Round(qty * sellPrice - Amount, 2)` // VAT portion
- Invoice totals:
  - `TotalAmount = sum(item.Amount + item.VatAmount)` = sum of VAT-inclusive line totals
  - `VatAmount   = sum(item.VatAmount)`

## InvoiceNo Format

```
ABB-{taxYear}{taxMonth:00}-{seq:000}
```
Example: `ABB-256806-001` for Thai year 2568, month 6, first invoice.

## Usage Example (Frontend ViewModel)

```csharp
// 1. Check if invoices already exist for this period
var existingCount = await _invoiceGeneratorService.GetExistingCountAsync(taxYear, taxMonth);
if (existingCount > 0)
{
    // Show confirmation dialog: "มี {existingCount} ใบแล้ว ต้องการแทนที่หรือไม่?"
    var confirmed = await ShowConfirmDialog(...);
    if (!confirmed) return;
}

// 2. Build PosStockLine list from MonthlyStock data (only rows where SellPosQty > 0)
var lines = monthlyStocks
    .Where(s => s.SellPosQty > 0)
    .Select(s => new PosStockLine(s.ProductName, s.SellPosQty, s.SellPrice))
    .ToList();

// 3. Generate invoices (replace existing if any)
var result = await _invoiceGeneratorService.GenerateAsync(
    taxYear, taxMonth, lines, replaceExisting: existingCount > 0);

if (result is null)
{
    StatusMessage = "ไม่มียอด POS ในงวดนี้";
    return;
}

StatusMessage = $"สร้าง {result.InvoiceCount} ใบ รวม {result.TotalAmount:N2} บาท (VAT {result.VatAmount:N2})";
```
