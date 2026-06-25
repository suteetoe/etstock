using ETStock.Models;

namespace ETStock.Services;

public interface IInvoicePrintService
{
    Task<InvoiceDocumentModel> BuildAsync(int invoiceId, CancellationToken ct = default);
}
