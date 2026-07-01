using ClosedXML.Excel;

namespace ETStock.Services;

public sealed class ExcelImportService : IExcelImportService
{
    private static readonly string[] RequiredColumns =
        ["ชื่อสินค้า", "ยอดยกมา", "ราคาขาย", "ต้นทุนสินค้า"];

    public IReadOnlyList<ExcelImportRecord> ReadStockMaster(string filePath)
    {
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();

        var lastHeaderCol = ws.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
        var colMap = new Dictionary<string, int>();
        for (int c = 1; c <= lastHeaderCol; c++)
        {
            var text = ws.Cell(1, c).GetString().Trim();
            if (!string.IsNullOrEmpty(text))
                colMap[text] = c;
        }

        foreach (var col in RequiredColumns)
        {
            if (!colMap.ContainsKey(col))
                throw new InvalidOperationException($"ไม่พบคอลัมน์ '{col}' ในไฟล์ Excel");
        }

        var result = new List<ExcelImportRecord>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int r = 2; r <= lastRow; r++)
        {
            var name = ws.Cell(r, colMap["ชื่อสินค้า"]).GetString().Trim();
            if (string.IsNullOrEmpty(name)) continue;

            var openingQty = GetDecimal(ws.Cell(r, colMap["ยอดยกมา"]));
            var sellPrice = GetDecimal(ws.Cell(r, colMap["ราคาขาย"]));
            var costPrice = GetDecimal(ws.Cell(r, colMap["ต้นทุนสินค้า"]));

            result.Add(new ExcelImportRecord(name, openingQty, sellPrice, costPrice));
        }

        return result;
    }

    private static decimal GetDecimal(IXLCell cell)
    {
        if (cell.IsEmpty()) return 0m;
        return cell.TryGetValue<decimal>(out var v) ? v : 0m;
    }
}
