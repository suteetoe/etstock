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
    /// - seedRunningNo: เลขที่เริ่มต้น (BookNo คำนวณอัตโนมัติจากสูตร (runningNo-1)/50+1)
    /// </summary>
    Task<GenerateInvoicesResult?> GenerateAsync(
        int taxYear,
        int taxMonth,
        IReadOnlyList<PosStockLine> stockLines,
        bool replaceExisting = false,
        int? seedRunningNo = null,
        CancellationToken ct = default);

    /// <summary>
    /// คืนค่า (BookNo, RunningNo) ล่าสุด (ทุกงวด) สำหรับให้ UI แสดงผลก่อน generate — null ถ้ายังไม่มี
    /// </summary>
    Task<(int BookNo, int RunningNo)?> GetLatestRunningAsync(CancellationToken ct = default);
}
