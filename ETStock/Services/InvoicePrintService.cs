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

        var company = await _companyRepo.GetAsync(ct)
            ?? throw new InvalidOperationException("ยังไม่ได้ตั้งค่าข้อมูลบริษัท กรุณาไปที่เมนูตั้งค่าและบันทึกข้อมูลบริษัทก่อนพิมพ์");

        return Build(invoice, company);
    }

    public async Task<byte[]> GeneratePdfAsync(int invoiceId, CancellationToken ct = default)
    {
        var doc = await BuildAsync(invoiceId, ct);
        return await Task.Run(() => new InvoicePdfDocument(doc).GeneratePdf(), ct);
    }

    public async Task<byte[]> GenerateAllPdfAsync(int taxYear, int taxMonth, CancellationToken ct = default)
    {
        var invoices = await _invoiceRepo.GetByPeriodAsync(taxYear, taxMonth);
        ct.ThrowIfCancellationRequested();

        if (invoices.Count == 0)
        {
            return Array.Empty<byte>();
        }

        var company = await _companyRepo.GetAsync(ct)
            ?? throw new InvalidOperationException("ยังไม่ได้ตั้งค่าข้อมูลบริษัท กรุณาไปที่เมนูตั้งค่าและบันทึกข้อมูลบริษัทก่อนพิมพ์");
        ct.ThrowIfCancellationRequested();

        var docs = invoices
            .Select(invoice => Build(invoice, company))
            .ToList();

        return await Task.Run(() => new MultiInvoicePdfDocument(docs).GeneratePdf(), ct);
    }

    private static InvoiceDocumentModel Build(AbbrInvoice invoice, Company company)
    {
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
            invoice.RunningNo,
            company.PhoneNumber);
    }
}
