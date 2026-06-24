using ETStock.Data;
using ETStock.Data.Repositories;
using ETStock.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ETStock.Tests;

public class MonthlyStockRepositoryTests
{
    private static AppDbContext CreateDb(string? databaseName = null) => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task SaveAsync_InsertsUpdatesAndReloadsMonthlyStock()
    {
        var databaseName = Guid.NewGuid().ToString();
        int productId;

        await using (var db = CreateDb(databaseName))
        {
            var product = CreateProduct("P001");
            db.Products.Add(product);
            await db.SaveChangesAsync();
            productId = product.Id;

            var repository = new MonthlyStockRepository(db);
            await repository.SaveAsync(
                new MonthlyStockInput(productId, 2026, 6, 10, 5, 2, 1));
            var updated = await repository.SaveAsync(
                new MonthlyStockInput(productId, 2026, 6, 20, 4, 3, 2));

            Assert.Equal(19, updated.ClosingQty);
            Assert.Single(await db.MonthlyStocks.ToListAsync());
        }

        await using (var db = CreateDb(databaseName))
        {
            var row = Assert.Single(await new MonthlyStockRepository(db).GetPeriodAsync(2026, 6));

            Assert.NotNull(row.MonthlyStock);
            Assert.Equal(20, row.MonthlyStock.OpeningQty);
            Assert.Equal(4, row.MonthlyStock.BuyQty);
            Assert.Equal(3, row.MonthlyStock.SellFullQty);
            Assert.Equal(2, row.MonthlyStock.SellPosQty);
            Assert.Equal(19, row.MonthlyStock.ClosingQty);
        }
    }

    [Fact]
    public async Task GetPeriodAsync_LoadsProductsAndExcludesStockFromOtherPeriods()
    {
        await using var db = CreateDb();
        var first = CreateProduct("P001");
        var second = CreateProduct("P002");
        db.Products.AddRange(first, second);
        await db.SaveChangesAsync();
        db.MonthlyStocks.AddRange(
            new MonthlyStock
            {
                ProductId = first.Id,
                Year = 2026,
                Month = 6,
                OpeningQty = 12
            },
            new MonthlyStock
            {
                ProductId = first.Id,
                Year = 2026,
                Month = 5,
                OpeningQty = 99
            });
        await db.SaveChangesAsync();

        var rows = await new MonthlyStockRepository(db).GetPeriodAsync(2026, 6);

        Assert.Equal(2, rows.Count);
        var firstRow = Assert.Single(rows, row => row.Id == first.Id);
        Assert.Equal(12, firstRow.MonthlyStock?.OpeningQty);
        var secondRow = Assert.Single(rows, row => row.Id == second.Id);
        Assert.Null(secondRow.MonthlyStock);
    }

    [Theory]
    [InlineData(2026, 6, 2026, 5)]
    [InlineData(2027, 1, 2026, 12)]
    public async Task CarryForwardAsync_ImportsPreviousClosingAcrossPeriodBoundaries(
        int targetYear,
        int targetMonth,
        int previousYear,
        int previousMonth)
    {
        await using var db = CreateDb();
        var product = CreateProduct("P001");
        db.Products.Add(product);
        await db.SaveChangesAsync();
        db.MonthlyStocks.Add(new MonthlyStock
        {
            ProductId = product.Id,
            Year = previousYear,
            Month = previousMonth,
            OpeningQty = 10,
            BuyQty = 8,
            SellFullQty = 3,
            SellPosQty = 2
        });
        await db.SaveChangesAsync();

        var carried = await new MonthlyStockRepository(db)
            .CarryForwardAsync(targetYear, targetMonth);

        var result = Assert.Single(carried);
        Assert.Equal(13, result.OpeningQty);
        Assert.Equal(13, result.ClosingQty);

        var saved = await db.MonthlyStocks.SingleAsync(
            stock => stock.Year == targetYear && stock.Month == targetMonth);
        Assert.Equal(13, saved.OpeningQty);
    }

    [Fact]
    public async Task CarryForwardAsync_UpdatesOpeningWithoutReplacingCurrentTransactions()
    {
        await using var db = CreateDb();
        var product = CreateProduct("P001");
        db.Products.Add(product);
        await db.SaveChangesAsync();
        db.MonthlyStocks.AddRange(
            new MonthlyStock
            {
                ProductId = product.Id,
                Year = 2026,
                Month = 5,
                OpeningQty = 10,
                BuyQty = 5,
                SellFullQty = 2,
                SellPosQty = 1
            },
            new MonthlyStock
            {
                ProductId = product.Id,
                Year = 2026,
                Month = 6,
                OpeningQty = 99,
                BuyQty = 7,
                SellFullQty = 3,
                SellPosQty = 2
            });
        await db.SaveChangesAsync();

        var carried = Assert.Single(
            await new MonthlyStockRepository(db).CarryForwardAsync(2026, 6));

        Assert.Equal(12, carried.OpeningQty);
        Assert.Equal(7, carried.BuyQty);
        Assert.Equal(3, carried.SellFullQty);
        Assert.Equal(2, carried.SellPosQty);
        Assert.Equal(14, carried.ClosingQty);
    }

    [Fact]
    public void ClosingQty_CalculatesOpeningPlusBuyMinusSales()
    {
        var stock = new MonthlyStock
        {
            OpeningQty = 10,
            BuyQty = 8,
            SellFullQty = 3,
            SellPosQty = 2
        };

        Assert.Equal(13, stock.ClosingQty);
    }

    private static Product CreateProduct(string code) => new()
    {
        Code = code,
        Name = $"Product {code}",
        Unit = "piece",
        CostPrice = 100,
        SellPrice = 150
    };
}
