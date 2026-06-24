using ETStock.Data;
using ETStock.Data.Repositories;
using ETStock.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ETStock.Tests;

public class ProductRepositoryTests
{
    private static AppDbContext CreateDb() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task GetAllAsync_ReturnsAllProducts()
    {
        await using var db = CreateDb();
        var first = CreateProduct("P001");
        var second = CreateProduct("P002");
        db.Products.AddRange(first, second);
        await db.SaveChangesAsync();
        var repository = new ProductRepository(db);

        var products = await repository.GetAllAsync();

        Assert.Equal(2, products.Count);
        Assert.Contains(products, product => product.Id == first.Id);
        Assert.Contains(products, product => product.Id == second.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProduct_WhenFound()
    {
        await using var db = CreateDb();
        var product = CreateProduct();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var repository = new ProductRepository(db);

        var result = await repository.GetByIdAsync(product.Id);

        Assert.NotNull(result);
        AssertProduct(product, result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        await using var db = CreateDb();
        var repository = new ProductRepository(db);

        var result = await repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_AddsProduct()
    {
        await using var db = CreateDb();
        var repository = new ProductRepository(db);
        var product = CreateProduct();

        var result = await repository.AddAsync(product);

        Assert.Equal(ProductWriteResult.Success, result);
        var saved = await db.Products.SingleAsync();
        AssertProduct(product, saved);
    }

    [Fact]
    public async Task AddAsync_ReturnsDuplicateCode_WhenCodeAlreadyExists()
    {
        await using var db = CreateDb();
        db.Products.Add(CreateProduct("P001"));
        await db.SaveChangesAsync();
        var repository = new ProductRepository(db);

        var result = await repository.AddAsync(CreateProduct("P001"));

        Assert.Equal(ProductWriteResult.DuplicateCode, result);
        Assert.Single(await db.Products.ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_UpdatesProduct_WhenFound()
    {
        await using var db = CreateDb();
        var original = CreateProduct();
        db.Products.Add(original);
        await db.SaveChangesAsync();
        var repository = new ProductRepository(db);
        var updated = new Product
        {
            Id = original.Id,
            Code = "P999",
            Name = "Updated Product",
            Unit = "box",
            CostPrice = 125.50m,
            SellPrice = 175.75m
        };

        var result = await repository.UpdateAsync(updated);

        Assert.Equal(ProductWriteResult.Success, result);
        var saved = await db.Products.SingleAsync();
        AssertProduct(updated, saved);
    }

    [Fact]
    public async Task UpdateAsync_DoesNothing_WhenNotFound()
    {
        await using var db = CreateDb();
        var repository = new ProductRepository(db);

        var result = await repository.UpdateAsync(CreateProduct());

        Assert.Equal(ProductWriteResult.NotFound, result);
        Assert.Empty(await db.Products.ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_DeletesProduct_WhenFound()
    {
        await using var db = CreateDb();
        var product = CreateProduct();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var repository = new ProductRepository(db);

        var result = await repository.DeleteAsync(product.Id);

        Assert.True(result);
        Assert.Empty(await db.Products.ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenNotFound()
    {
        await using var db = CreateDb();
        var repository = new ProductRepository(db);

        var result = await repository.DeleteAsync(999);

        Assert.False(result);
        Assert.Empty(await db.Products.ToListAsync());
    }

    [Fact]
    public async Task GetByCodeAsync_ReturnsMatchingProduct()
    {
        await using var db = CreateDb();
        var expected = CreateProduct("MATCH");
        db.Products.AddRange(CreateProduct("OTHER"), expected);
        await db.SaveChangesAsync();
        var repository = new ProductRepository(db);

        var result = await repository.GetByCodeAsync("MATCH");

        Assert.NotNull(result);
        AssertProduct(expected, result);
    }

    [Fact]
    public async Task GetAllWithMonthlyStockAsync_ReturnsRequestedPeriodOnly()
    {
        await using var db = CreateDb();
        var product = CreateProduct();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        db.MonthlyStocks.AddRange(
            new MonthlyStock
            {
                ProductId = product.Id,
                Year = 2026,
                Month = 6,
                OpeningQty = 10,
                BuyQty = 5,
                SellFullQty = 2,
                SellPosQty = 1
            },
            new MonthlyStock
            {
                ProductId = product.Id,
                Year = 2026,
                Month = 5,
                OpeningQty = 99
            });
        await db.SaveChangesAsync();
        var repository = new ProductRepository(db);

        var result = await repository.GetAllWithMonthlyStockAsync(2026, 6);

        var row = Assert.Single(result);
        Assert.NotNull(row.MonthlyStock);
        Assert.Equal(12, row.MonthlyStock.ClosingQty);
    }

    [Fact]
    public async Task UpsertMonthlyStockAsync_InsertsThenUpdatesSamePeriod()
    {
        await using var db = CreateDb();
        var product = CreateProduct();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        var repository = new ProductRepository(db);

        await repository.UpsertMonthlyStockAsync(
            new MonthlyStockInput(product.Id, 2026, 6, 10, 5, 2, 1));
        var result = await repository.UpsertMonthlyStockAsync(
            new MonthlyStockInput(product.Id, 2026, 6, 20, 4, 3, 2));

        Assert.Single(await db.MonthlyStocks.ToListAsync());
        Assert.Equal(19, result.ClosingQty);
    }

    [Theory]
    [InlineData(10, 5, 2, 1, 12)]
    [InlineData(10, 0, 5, 5, 0)]
    [InlineData(2, 1, 4, 3, -4)]
    public void ClosingQty_ComputesPositiveZeroAndNegativeValues(
        decimal opening,
        decimal buy,
        decimal sellFull,
        decimal sellPos,
        decimal expected)
    {
        var stock = new MonthlyStockSnapshot(
            1,
            1,
            2026,
            6,
            opening,
            buy,
            sellFull,
            sellPos);

        Assert.Equal(expected, stock.ClosingQty);
    }

    private static Product CreateProduct(string code = "P001") => new()
    {
        Code = code,
        Name = "Test Product",
        Unit = "piece",
        CostPrice = 100.25m,
        SellPrice = 150.50m
    };

    private static void AssertProduct(Product expected, Product actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Code, actual.Code);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Unit, actual.Unit);
        Assert.Equal(expected.CostPrice, actual.CostPrice);
        Assert.Equal(expected.SellPrice, actual.SellPrice);
    }
}
