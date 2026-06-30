namespace ETStock.Services;

public record PosStockLine(string ProductName, decimal SellPosQty, decimal SellPrice);
public record GenerateInvoicesResult(int InvoiceCount, decimal TotalAmount, decimal VatAmount);

public interface IInvoiceGeneratorService
{
    /// <summary>
    /// จำนวนใบกำกับที่มีอยู่ในงวดนั้น (สำหรับ UI ถาม confirm)
    /// </summary>
    Task<int> GetExistingCountAsync(int taxYear, int taxMonth, CancellationToken ct = default);

    /// <summary>
    /// สร้างใบกำกับสุ่มจาก POS qty
    /// - ถ้า replaceExisting = true ให้ลบใบเดิมทั้งหมดก่อน
    /// - คืน null ถ้าไม่มี stockLine ใดที่ SellPosQty > 0
    /// </summary>
    Task<GenerateInvoicesResult?> GenerateAsync(
        int taxYear,
        int taxMonth,
        IReadOnlyList<PosStockLine> stockLines,
        bool replaceExisting = false,
        CancellationToken ct = default);
}
