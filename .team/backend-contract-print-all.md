# Backend Contract — Phase Print All Invoices

## Namespace

```csharp
ETStock.Services
```

## Interface: IInvoicePrintService

```csharp
public interface IInvoicePrintService
{
    Task<InvoiceDocumentModel> BuildAsync(int invoiceId, CancellationToken ct = default);
    Task<byte[]> GeneratePdfAsync(int invoiceId, CancellationToken ct = default);
    Task<byte[]> GenerateAllPdfAsync(int taxYear, int taxMonth, CancellationToken ct = default);
}
```

## Behavior

- `GenerateAllPdfAsync` loads all abbreviated invoices for the requested `(taxYear, taxMonth)`.
- If no invoices exist for the period, it returns `Array.Empty<byte>()`.
- If invoices exist, it returns one QuestPDF-generated PDF byte array.
- The generated PDF contains one invoice per page.
- Existing single-invoice methods remain unchanged.

## Usage Example (Frontend ViewModel)

```csharp
var pdfBytes = await _invoicePrintService.GenerateAllPdfAsync(
    SelectedYear,
    SelectedMonth,
    cancellationToken);

if (pdfBytes.Length == 0)
{
    StatusMessage = "ไม่มีใบกำกับในงวดนี้";
    return;
}

var path = Path.Combine(
    Path.GetTempPath(),
    $"etstock_invoices_{SelectedYear}_{SelectedMonth:00}.pdf");

await File.WriteAllBytesAsync(path, pdfBytes, cancellationToken);
```

## DI Registration

No new DI registration is required. Continue using the existing scoped registration:

```csharp
services.AddScoped<IInvoicePrintService, InvoicePrintService>();
```
