# Backend Brief — Phase 5 Printing (P5P-B)

## Context

ETStock is an Avalonia 11 (.NET 10) desktop app for Thai retail abbreviated tax invoices.
Stack: Avalonia 11.3.11, CommunityToolkit.Mvvm 8.2.1, EF Core 10 + Npgsql, PostgreSQL, xUnit 2.9.3.

You are on branch `backend/phase5-print-service` (from `develop`). Write code to the files listed below. Do NOT push or create a PR — the orchestrator handles git.

## Existing models you will use

```
ETStock/Models/AbbrInvoice.cs       — Id, InvoiceNo, InvoiceDate, TaxYear, TaxMonth, TotalAmount, VatAmount, Items
ETStock/Models/AbbrInvoiceItem.cs   — Id, AbbrInvoiceId, ProductId, Product(navigation), Qty, Amount, VatAmount
ETStock/Models/Product.cs           — Id, Code, Name, Unit, CostPrice, SellPrice
ETStock/Models/Company.cs           — Id, Name, TaxId, Address, BranchName, BranchCode, InvoicePrefix, VatRate
ETStock/Data/Repositories/IAbbrInvoiceRepository.cs — GetByIdAsync(int) Task<AbbrInvoice?>
ETStock/Data/Repositories/ICompanyRepository.cs      — GetAsync(ct) Task<Company?>
```

DI is registered in `Program.cs` → `ConfigureServices`. Look at the file before editing.

## Files to CREATE

### 1. `ETStock/Models/InvoiceDocumentModel.cs`
```csharp
namespace ETStock.Models;

public record InvoiceDocumentLine(
    string ProductCode,
    string ProductName,
    decimal Qty,
    decimal Amount,
    decimal VatAmount);

public record InvoiceDocumentModel(
    string CompanyName,
    string CompanyTaxId,
    string CompanyAddress,
    string CompanyBranch,
    string CompanyBranchCode,
    string InvoiceNo,
    DateTime InvoiceDate,
    int TaxYear,
    int TaxMonth,
    IReadOnlyList<InvoiceDocumentLine> Lines,
    decimal SubTotal,
    decimal VatTotal,
    decimal GrandTotal);
```

### 2. `ETStock/Services/IInvoicePrintService.cs`
```csharp
using ETStock.Models;

namespace ETStock.Services;

public interface IInvoicePrintService
{
    Task<InvoiceDocumentModel> BuildAsync(int invoiceId, CancellationToken ct = default);
}
```

### 3. `ETStock/Services/InvoicePrintService.cs`
```csharp
using ETStock.Data.Repositories;
using ETStock.Models;

namespace ETStock.Services;

public sealed class InvoicePrintService : IInvoicePrintService
{
    private readonly IAbbrInvoiceRepository _invoiceRepo;
    private readonly ICompanyRepository _companyRepo;

    public InvoicePrintService(IAbbrInvoiceRepository invoiceRepo, ICompanyRepository companyRepo)
    {
        _invoiceRepo = invoiceRepo;
        _companyRepo = companyRepo;
    }

    public async Task<InvoiceDocumentModel> BuildAsync(int invoiceId, CancellationToken ct = default)
    {
        var invoice = await _invoiceRepo.GetByIdAsync(invoiceId)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} not found.");

        var company = await _companyRepo.GetAsync(ct) ?? new Company();

        var lines = invoice.Items
            .Select(item => new InvoiceDocumentLine(
                item.Product?.Code ?? string.Empty,
                item.Product?.Name ?? $"สินค้า {item.ProductId}",
                item.Qty,
                item.Amount,
                item.VatAmount))
            .ToList();

        var subTotal = lines.Sum(l => l.Amount);
        var vatTotal = lines.Sum(l => l.VatAmount);

        return new InvoiceDocumentModel(
            company.Name,
            company.TaxId,
            company.Address,
            company.BranchName,
            company.BranchCode,
            invoice.InvoiceNo,
            invoice.InvoiceDate,
            invoice.TaxYear,
            invoice.TaxMonth,
            lines,
            subTotal,
            vatTotal,
            subTotal + vatTotal);
    }
}
```

## Files to MODIFY

### 4. `ETStock/Program.cs`
Add `using ETStock.Services;` at the top (with other usings).
In `ConfigureServices`, after `services.AddScoped<IAbbrInvoiceRepository, AbbrInvoiceRepository>();`, add:
```csharp
services.AddScoped<IInvoicePrintService, InvoicePrintService>();
```

## Verification

After writing all files, run:
```
dotnet build ETStock/ETStock.csproj
```
It must succeed with 0 errors. Fix any compilation errors before finishing.

## DoD

- 3 new files created (InvoiceDocumentModel.cs, IInvoicePrintService.cs, InvoicePrintService.cs)
- Program.cs has the DI registration
- `dotnet build ETStock/ETStock.csproj` → 0 errors
