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

    /// <summary>
    /// คืนค่า (BookNo, RunningNo) ของแถวที่มี RunningNo สูงสุด (ทุกงวด) หรือ null ถ้ายังไม่มีแถวใดมี RunningNo
    /// </summary>
    Task<(int BookNo, int RunningNo)?> GetLatestRunningAsync(CancellationToken ct = default);

    /// <summary>
    /// จำนวนใบกำกับที่อยู่ในเล่มที่ระบุ
    /// </summary>
    Task<int> CountByBookNoAsync(int bookNo, CancellationToken ct = default);
}
