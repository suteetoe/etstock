namespace ETStock.Services;

public sealed record ExcelImportRecord(
    string ProductName,
    decimal OpeningQty,
    decimal SellPrice,
    decimal CostPrice);

public interface IExcelImportService
{
    IReadOnlyList<ExcelImportRecord> ReadStockMaster(string filePath);
}
