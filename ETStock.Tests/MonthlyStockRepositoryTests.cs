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

        await using (var db = CreateDb(databaseName))
        {
            var repository = new MonthlyStockRepository(db);
            await repository.SaveAsync(
                new MonthlyStockInput("สินค้า A", "ชิ้น", 100m, 150m, 2026, 6, 10, 5, 2, 1));
            var updated = await repository.SaveAsync(
                new MonthlyStockInput("สินค้า A", "ชิ้น", 100m, 150m, 2026, 6, 20, 4, 3, 2));

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
        db.MonthlyStocks.AddRange(
            new MonthlyStock
            {
                ProductName = "สินค้า A",
                Unit = "ชิ้น",
                CostPrice = 100m,
                SellPrice = 150m,
                Year = 2026,
                Month = 6,
                OpeningQty = 12
            },
            new MonthlyStock
            {
                ProductName = "สินค้า A",
                Unit = "ชิ้น",
                CostPrice = 100m,
                SellPrice = 150m,
                Year = 2026,
                Month = 5,
                OpeningQty = 99
            },
            new MonthlyStock
            {
                ProductName = "สินค้า B",
                Unit = "กล่อง",
                CostPrice = 200m,
                SellPrice = 300m,
                Year = 2026,
                Month = 6,
                OpeningQty = 5
            });
        await db.SaveChangesAsync();

        var rows = await new MonthlyStockRepository(db).GetPeriodAsync(2026, 6);

        Assert.Equal(2, rows.Count);
        var rowA = Assert.Single(rows, row => row.ProductName == "สินค้า A");
        Assert.Equal(12, rowA.MonthlyStock?.OpeningQty);
        var rowB = Assert.Single(rows, row => row.ProductName == "สินค้า B");
        Assert.Equal(5, rowB.MonthlyStock?.OpeningQty);
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
        db.MonthlyStocks.Add(new MonthlyStock
        {
            ProductName = "สินค้า A",
            Unit = "ชิ้น",
            CostPrice = 100m,
            SellPrice = 150m,
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
    public async Task CarryForwardAsync_ChainsClosingAcrossNormalMonthAndYearBoundary()
    {
        var databaseName = Guid.NewGuid().ToString();

        await using (var db = CreateDb(databaseName))
        {
            db.MonthlyStocks.Add(new MonthlyStock
            {
                ProductName = "สินค้า A",
                Unit = "ชิ้น",
                CostPrice = 100m,
                SellPrice = 150m,
                Year = 2026,
                Month = 11,
                OpeningQty = 10,
                BuyQty = 8,
                SellFullQty = 3,
                SellPosQty = 2
            });
            await db.SaveChangesAsync();

            var repository = new MonthlyStockRepository(db);
            var december = Assert.Single(
                await repository.CarryForwardAsync(2026, 12));
            Assert.Equal(13, december.OpeningQty);

            await repository.SaveAsync(
                new MonthlyStockInput("สินค้า A", "ชิ้น", 100m, 150m, 2026, 12, 13, 7, 4, 1));

            var january = Assert.Single(
                await repository.CarryForwardAsync(2027, 1));
            Assert.Equal(15, january.OpeningQty);
            Assert.Equal(15, january.ClosingQty);
        }

        await using (var db = CreateDb(databaseName))
        {
            var rows = await db.MonthlyStocks
                .OrderBy(stock => stock.Year)
                .ThenBy(stock => stock.Month)
                .ToListAsync();

            Assert.Collection(
                rows,
                november =>
                {
                    Assert.Equal((2026, 11), (november.Year, november.Month));
                    Assert.Equal(13, november.ClosingQty);
                },
                december =>
                {
                    Assert.Equal((2026, 12), (december.Year, december.Month));
                    Assert.Equal(15, december.ClosingQty);
                },
                january =>
                {
                    Assert.Equal((2027, 1), (january.Year, january.Month));
                    Assert.Equal(15, january.OpeningQty);
                });
        }
    }

    [Fact]
    public async Task CarryForwardAsync_UpdatesOpeningWithoutReplacingCurrentTransactions()
    {
        await using var db = CreateDb();
        db.MonthlyStocks.AddRange(
            new MonthlyStock
            {
                ProductName = "สินค้า A",
                Unit = "ชิ้น",
                CostPrice = 100m,
                SellPrice = 150m,
                Year = 2026,
                Month = 5,
                OpeningQty = 10,
                BuyQty = 5,
                SellFullQty = 2,
                SellPosQty = 1
            },
            new MonthlyStock
            {
                ProductName = "สินค้า A",
                Unit = "ชิ้น",
                CostPrice = 100m,
                SellPrice = 150m,
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
    public async Task DeleteAsync_RemovesStockByProductNameAndPeriod()
    {
        await using var db = CreateDb();
        db.MonthlyStocks.Add(new MonthlyStock
        {
            ProductName = "สินค้า A",
            Unit = "ชิ้น",
            CostPrice = 100m,
            SellPrice = 150m,
            Year = 2026,
            Month = 6,
            OpeningQty = 10
        });
        await db.SaveChangesAsync();

        var repository = new MonthlyStockRepository(db);
        var result = await repository.DeleteAsync("สินค้า A", 2026, 6);

        Assert.True(result);
        Assert.Empty(await db.MonthlyStocks.ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        await using var db = CreateDb();

        var repository = new MonthlyStockRepository(db);
        var result = await repository.DeleteAsync("ไม่มีสินค้านี้", 2026, 6);

        Assert.False(result);
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
}
