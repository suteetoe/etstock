using ETStock.Data.Repositories;
using ETStock.Models;
using QuestPDF.Fluent;

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
                item.ProductName,
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
            subTotal + vatTotal,
            invoice.BookNo,
            invoice.RunningNo);
    }

    public async Task<byte[]> GeneratePdfAsync(int invoiceId, CancellationToken ct = default)
    {
        var doc = await BuildAsync(invoiceId, ct);
        return await Task.Run(() => new InvoicePdfDocument(doc).GeneratePdf(), ct);
    }
}
