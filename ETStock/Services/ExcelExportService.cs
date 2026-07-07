using ClosedXML.Excel;
using ETStock.ViewModels;

namespace ETStock.Services;

public sealed class ExcelExportService : IExcelExportService
{
    private static readonly string[] Headers =
    [
        "ลำดับ",
        "ชื่อสินค้า",
        "ยอดยกมา",
        "ซื้อเข้า",
        "ขายออก",
        "บิลเต็ม",
        "คงเหลือ",
        "ราคาขาย",
        "จำนวนเงิน",
        "ต้นทุนสินค้า",
        "ต้นทุนรวม",
        "มูลค่าสินค้าคงเหลือ",
        "ยอดคงเหลือยกไป"
    ];

    public void WriteStock(IReadOnlyList<MonthlyStockRowViewModel> rows, int year, int month, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Stock_{year}_{month:00}");

        for (var c = 0; c < Headers.Length; c++)
        {
            ws.Cell(1, c + 1).Value = Headers[c];
        }

        var headerRange = ws.Range(1, 1, 1, Headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var excelRow = i + 2;

            ws.Cell(excelRow, 1).Value = row.LineNumber;
            ws.Cell(excelRow, 2).Value = row.Name;
            ws.Cell(excelRow, 3).Value = row.OpeningQty;
            ws.Cell(excelRow, 4).Value = row.BuyQty;
            ws.Cell(excelRow, 5).Value = row.SellPosQty;
            ws.Cell(excelRow, 6).Value = row.SellFullQty;
            ws.Cell(excelRow, 7).Value = row.ClosingQty;
            ws.Cell(excelRow, 8).Value = row.SellPrice;
            ws.Cell(excelRow, 9).Value = row.SalesAmount;
            ws.Cell(excelRow, 10).Value = row.CostPrice;
            ws.Cell(excelRow, 11).Value = row.TotalCost;
            ws.Cell(excelRow, 12).Value = row.ClosingValue;
            ws.Cell(excelRow, 13).Value = row.CarryForwardQty;
        }

        if (rows.Count > 0)
            ws.Range(2, 3, rows.Count + 1, 13).Style.NumberFormat.Format = "#,##0.00";

        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }
}
