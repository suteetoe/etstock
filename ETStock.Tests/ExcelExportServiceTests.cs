using ClosedXML.Excel;
using ETStock.Data.Repositories;
using ETStock.Services;
using ETStock.ViewModels;
using Xunit;

namespace ETStock.Tests;

public class ExcelExportServiceTests
{
    [Fact]
    public void WriteStock_CreatesFileWithCorrectHeaders()
    {
        var service = new ExcelExportService();
        var path = TempPath();

        try
        {
            service.WriteStock([CreateRow()], 2026, 6, path);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.First();

            string[] expectedHeaders =
            [
                "ลำดับ", "ชื่อสินค้า", "ยอดยกมา", "ซื้อเข้า",
                "ขายออก", "บิลเต็ม", "คงเหลือ", "ราคาขาย",
                "จำนวนเงิน", "ต้นทุนสินค้า", "ต้นทุนรวม", "มูลค่าสินค้าคงเหลือ"
            ];

            for (int c = 1; c <= 12; c++)
                Assert.Equal(expectedHeaders[c - 1], ws.Cell(1, c).GetString());

            Assert.True(ws.Cell(1, 1).Style.Font.Bold);
            Assert.Equal(XLFillPatternValues.Solid, ws.Cell(1, 1).Style.Fill.PatternType);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void WriteStock_WritesCorrectRowData()
    {
        var service = new ExcelExportService();
        var row = CreateRow(openingQty: 10, buyQty: 5, sellFullQty: 3, sellPosQty: 1, costPrice: 100m, sellPrice: 150m);
        row.LineNumber = 1;
        var path = TempPath();

        try
        {
            service.WriteStock([row], 2026, 6, path);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.First();

            // SalesQty=3+1=4, ClosingQty=10+5-3-1=11, SalesAmount=4*150=600, TotalCost=4*100=400, ClosingValue=11*100=1100
            Assert.Equal(1.0, ws.Cell(2, 1).GetValue<double>());    // LineNumber
            Assert.Equal("สินค้า A", ws.Cell(2, 2).GetString());    // Name
            Assert.Equal(10.0, ws.Cell(2, 3).GetValue<double>());   // OpeningQty
            Assert.Equal(5.0, ws.Cell(2, 4).GetValue<double>());    // BuyQty
            Assert.Equal(4.0, ws.Cell(2, 5).GetValue<double>());    // SalesQty
            Assert.Equal(3.0, ws.Cell(2, 6).GetValue<double>());    // SellFullQty
            Assert.Equal(11.0, ws.Cell(2, 7).GetValue<double>());   // ClosingQty
            Assert.Equal(150.0, ws.Cell(2, 8).GetValue<double>());  // SellPrice
            Assert.Equal(600.0, ws.Cell(2, 9).GetValue<double>());  // SalesAmount
            Assert.Equal(100.0, ws.Cell(2, 10).GetValue<double>()); // CostPrice
            Assert.Equal(400.0, ws.Cell(2, 11).GetValue<double>()); // TotalCost
            Assert.Equal(1100.0, ws.Cell(2, 12).GetValue<double>()); // ClosingValue
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void WriteStock_TotalCostIsSalesQtyTimesCostPrice()
    {
        var service = new ExcelExportService();
        var row = CreateRow(sellFullQty: 5, sellPosQty: 2, costPrice: 80m);
        var path = TempPath();

        try
        {
            service.WriteStock([row], 2026, 6, path);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.First();

            // SalesQty=5+2=7, CostPrice=80, TotalCost=7*80=560
            var salesQty = ws.Cell(2, 5).GetValue<double>();
            var costPrice = ws.Cell(2, 10).GetValue<double>();
            var totalCost = ws.Cell(2, 11).GetValue<double>();

            Assert.Equal(7.0, salesQty);
            Assert.Equal(80.0, costPrice);
            Assert.Equal(salesQty * costPrice, totalCost);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void WriteStock_AppliesNumberFormat()
    {
        var service = new ExcelExportService();
        var path = TempPath();

        try
        {
            service.WriteStock([CreateRow()], 2026, 6, path);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.First();

            for (int col = 3; col <= 12; col++)
            {
                var fmt = ws.Cell(2, col).Style.NumberFormat;
                Assert.True(
                    fmt.Format == "#,##0.00" || fmt.NumberFormatId == 4,
                    $"Column {col}: expected #,##0.00 format but got Format='{fmt.Format}' Id={fmt.NumberFormatId}");
            }
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static MonthlyStockRowViewModel CreateRow(
        string name = "สินค้า A",
        decimal openingQty = 10,
        decimal buyQty = 5,
        decimal sellFullQty = 2,
        decimal sellPosQty = 1,
        decimal costPrice = 100m,
        decimal sellPrice = 150m)
    {
        var product = new ProductWithStock(name, "ชิ้น", costPrice, sellPrice,
            new MonthlyStockSnapshot(1, name, 2026, 6, openingQty, buyQty, sellFullQty, sellPosQty));
        return new MonthlyStockRowViewModel(product);
    }

    private static string TempPath() =>
        Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.xlsx");
}
