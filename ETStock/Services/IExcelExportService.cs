using ETStock.ViewModels;

namespace ETStock.Services;

public interface IExcelExportService
{
    void WriteStock(IReadOnlyList<MonthlyStockRowViewModel> rows, int year, int month, string filePath);
}
