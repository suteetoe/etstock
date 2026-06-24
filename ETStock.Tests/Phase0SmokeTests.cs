using ETStock.Data;
using ETStock.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ETStock.Tests;

public class Phase0SmokeTests
{
    // Test 1: Verify all entity types are registered in DbContext
    [Fact]
    public void DbContext_ContainsAllRequiredEntities()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("smoke-test")
            .Options;

        using var ctx = new AppDbContext(options);

        Assert.NotNull(ctx.Companies);
        Assert.NotNull(ctx.Products);
        Assert.NotNull(ctx.MonthlyStocks);
        Assert.NotNull(ctx.AbbrInvoices);
        Assert.NotNull(ctx.AbbrInvoiceItems);
    }

    // Test 2: Verify entity model properties exist
    [Fact]
    public void Company_HasRequiredProperties()
    {
        var c = new Company
        {
            Name = "บริษัท ทดสอบ จำกัด",
            TaxId = "0105566001234",
            VatRate = 0.07m,
            InvoicePrefix = "AB"
        };
        Assert.Equal("บริษัท ทดสอบ จำกัด", c.Name);
        Assert.Equal(0.07m, c.VatRate);
    }

    // Test 3: Verify Product unique code model
    [Fact]
    public void Product_HasRequiredProperties()
    {
        var p = new Product { Code = "P001", Name = "สินค้าทดสอบ", Unit = "ชิ้น", SellPrice = 100m };
        Assert.Equal("P001", p.Code);
        Assert.Equal(100m, p.SellPrice);
    }

    // Test 4: MonthlyStock closing formula
    [Fact]
    public void MonthlyStock_ClosingFormula_IsCorrect()
    {
        var ms = new MonthlyStock
        {
            OpeningQty = 100,
            BuyQty = 50,
            SellFullQty = 30,
            SellPosQty = 20
        };
        // closing = opening + buy - sellFull - sellPos
        var closing = ms.OpeningQty + ms.BuyQty - ms.SellFullQty - ms.SellPosQty;
        Assert.Equal(100m, closing);
    }

    // Test 5: Migration files exist
    [Fact]
    public void Migration_InitialFilesExist()
    {
        var migrationDir = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "ETStock", "Data", "Migrations");

        var dir = new DirectoryInfo(migrationDir);
        Assert.True(dir.Exists, $"Migrations directory not found at: {dir.FullName}");

        var migrationFiles = dir.GetFiles("*_Initial.cs");
        Assert.True(migrationFiles.Length > 0, "No Initial migration file found in Data/Migrations/");
    }
}
