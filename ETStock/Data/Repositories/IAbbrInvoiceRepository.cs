using ETStock.Models;

namespace ETStock.Data.Repositories;

public interface IAbbrInvoiceRepository
{
    Task<List<AbbrInvoice>> GetByPeriodAsync(int taxYear, int taxMonth);
    Task<AbbrInvoice?> GetByIdAsync(int id);
    Task SaveAsync(AbbrInvoice invoice);
    Task DeleteAsync(int id);
    Task DeleteByPeriodAsync(int taxYear, int taxMonth, CancellationToken ct = default);
    Task<AbbrInvoiceSummary> GetPeriodSummaryAsync(int taxYear, int taxMonth);
}
