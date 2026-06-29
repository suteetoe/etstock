namespace ETStock.Services;

public record PosStockLine(string ProductName, decimal SellPosQty, decimal SellPrice);
public record GenerateInvoicesResult(int InvoiceCount, decimal TotalAmount, decimal VatAmount);

public interface IInvoiceGeneratorService
{
    Task<int> GetExistingCountAsync(int taxYear, int taxMonth, CancellationToken ct = default);

    Task<GenerateInvoicesResult?> GenerateAsync(
        int taxYear,
        int taxMonth,
        IReadOnlyList<PosStockLine> stockLines,
        bool replaceExisting = false,
        CancellationToken ct = default);
}
