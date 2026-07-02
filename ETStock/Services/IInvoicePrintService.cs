using ETStock.Models;

namespace ETStock.Services;

public interface IInvoicePrintService
{
    Task<InvoiceDocumentModel> BuildAsync(int invoiceId, CancellationToken ct = default);
    Task<byte[]> GeneratePdfAsync(int invoiceId, CancellationToken ct = default);
    Task<byte[]> GenerateAllPdfAsync(int taxYear, int taxMonth, CancellationToken ct = default);
}
