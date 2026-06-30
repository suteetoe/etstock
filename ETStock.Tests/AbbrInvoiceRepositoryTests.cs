using ETStock.Data;
using ETStock.Data.Repositories;
using ETStock.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ETStock.Tests;

public class AbbrInvoiceRepositoryTests
{
    private static AppDbContext CreateDb(string? databaseName = null) => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options);

    private static AbbrInvoice CreateInvoice(int taxYear, int taxMonth, string productName = "สินค้า ก", int itemCount = 2)
    {
        var invoice = new AbbrInvoice
        {
            InvoiceNo = $"INV-{taxYear}-{taxMonth}",
            InvoiceDate = new DateTime(taxYear, taxMonth, 1),
            TaxYear = taxYear,
            TaxMonth = taxMonth,
            TotalAmount = 107m * itemCount,
            VatAmount = 7m * itemCount
        };

        for (var i = 0; i < itemCount; i++)
        {
            invoice.Items.Add(new AbbrInvoiceItem
            {
                ProductName = productName,
                Qty = 1,
                Amount = 100m,
                VatAmount = 7m
            });
        }

        return invoice;
    }

    [Fact]
    public async Task SaveAsync_NewInvoice_AssignsId()
    {
        await using var db = CreateDb();

        var invoice = CreateInvoice(2566, 1, itemCount: 2);
        var repo = new AbbrInvoiceRepository(db);
        await repo.SaveAsync(invoice);

        Assert.True(invoice.Id > 0);
        Assert.Equal(2, await db.AbbrInvoiceItems.CountAsync());
    }

    [Fact]
    public async Task SaveAsync_UnspecifiedInvoiceDate_DoesNotThrow()
    {
        await using var db = CreateDb();

        var invoice = CreateInvoice(2566, 1, itemCount: 1);
        Assert.Equal(DateTimeKind.Unspecified, invoice.InvoiceDate.Kind);

        var repo = new AbbrInvoiceRepository(db);

        // Npgsql enforces DateTimeKind for timestamptz; InMemory guards the entity/repository path.
        var exception = await Record.ExceptionAsync(() => repo.SaveAsync(invoice));

        Assert.Null(exception);
        Assert.True(invoice.Id > 0);
    }

    [Fact]
    public async Task GetByPeriodAsync_ReturnsMatchingPeriod()
    {
        await using var db = CreateDb();

        var repo = new AbbrInvoiceRepository(db);
        await repo.SaveAsync(CreateInvoice(2566, 1));
        await repo.SaveAsync(CreateInvoice(2566, 2));

        var period1 = await repo.GetByPeriodAsync(2566, 1);
        var period2 = await repo.GetByPeriodAsync(2566, 2);

        Assert.Single(period1);
        Assert.Equal(1, period1[0].TaxMonth);
        Assert.Single(period2);
        Assert.Equal(2, period2[0].TaxMonth);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsWithItems()
    {
        await using var db = CreateDb();

        var invoice = CreateInvoice(2566, 1, itemCount: 2);
        var repo = new AbbrInvoiceRepository(db);
        await repo.SaveAsync(invoice);

        var result = await repo.GetByIdAsync(invoice.Id);

        Assert.NotNull(result);
        Assert.Equal(invoice.Id, result.Id);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.NotEmpty(item.ProductName));
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await using var db = CreateDb();
        var repo = new AbbrInvoiceRepository(db);

        var result = await repo.GetByIdAsync(99999);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_UpdateInvoice_ReplacesItems()
    {
        await using var db = CreateDb();

        var invoice = CreateInvoice(2566, 1, itemCount: 2);
        var repo = new AbbrInvoiceRepository(db);
        await repo.SaveAsync(invoice);

        Assert.Equal(2, await db.AbbrInvoiceItems.CountAsync());

        var updated = new AbbrInvoice
        {
            Id = invoice.Id,
            InvoiceNo = invoice.InvoiceNo,
            InvoiceDate = invoice.InvoiceDate,
            TaxYear = invoice.TaxYear,
            TaxMonth = invoice.TaxMonth,
            TotalAmount = 107m,
            VatAmount = 7m
        };
        updated.Items.Add(new AbbrInvoiceItem
        {
            ProductName = "สินค้า ก",
            Qty = 1,
            Amount = 100m,
            VatAmount = 7m
        });

        await repo.SaveAsync(updated);

        Assert.Equal(1, await db.AbbrInvoiceItems.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_RemovesInvoiceAndItems()
    {
        await using var db = CreateDb();

        var invoice = CreateInvoice(2566, 1, itemCount: 2);
        var repo = new AbbrInvoiceRepository(db);
        await repo.SaveAsync(invoice);

        Assert.Equal(1, await db.AbbrInvoices.CountAsync());
        Assert.Equal(2, await db.AbbrInvoiceItems.CountAsync());

        await repo.DeleteAsync(invoice.Id);

        Assert.Equal(0, await db.AbbrInvoices.CountAsync());
        Assert.Equal(0, await db.AbbrInvoiceItems.CountAsync());
    }

    [Fact]
    public async Task GetPeriodSummaryAsync_ReturnsCorrectTotals()
    {
        await using var db = CreateDb();

        var repo = new AbbrInvoiceRepository(db);
        // Two invoices in period (2566, 1); each has itemCount=2 -> TotalAmount=214m, VatAmount=14m
        await repo.SaveAsync(CreateInvoice(2566, 1, itemCount: 2));
        await repo.SaveAsync(CreateInvoice(2566, 1, itemCount: 2));
        // One invoice in a different period — must not be counted
        await repo.SaveAsync(CreateInvoice(2566, 2, itemCount: 3));

        var summary = await repo.GetPeriodSummaryAsync(2566, 1);

        Assert.Equal(2, summary.Count);
        Assert.Equal(214m * 2, summary.TotalAmount);  // 428m
        Assert.Equal(14m * 2, summary.VatAmount);     // 28m
    }

    [Fact]
    public async Task GetPeriodSummaryAsync_NoInvoices_ReturnsZero()
    {
        await using var db = CreateDb();
        var repo = new AbbrInvoiceRepository(db);

        var summary = await repo.GetPeriodSummaryAsync(2566, 1);

        Assert.Equal(0, summary.Count);
        Assert.Equal(0m, summary.TotalAmount);
        Assert.Equal(0m, summary.VatAmount);
    }

    [Fact]
    public async Task GetLatestRunningAsync_NoInvoices_ReturnsNull()
    {
        await using var db = CreateDb();
        var repo = new AbbrInvoiceRepository(db);

        var latest = await repo.GetLatestRunningAsync();

        Assert.Null(latest);
    }

    [Fact]
    public async Task GetLatestRunningAsync_NoRunningNoSet_ReturnsNull()
    {
        await using var db = CreateDb();
        var repo = new AbbrInvoiceRepository(db);

        // legacy invoices with no BookNo/RunningNo
        await repo.SaveAsync(CreateInvoice(2566, 1));
        await repo.SaveAsync(CreateInvoice(2566, 2));

        var latest = await repo.GetLatestRunningAsync();

        Assert.Null(latest);
    }

    [Fact]
    public async Task GetLatestRunningAsync_ReturnsMaxRunningNoRow()
    {
        await using var db = CreateDb();
        var repo = new AbbrInvoiceRepository(db);

        var inv1 = CreateInvoice(2566, 1);
        inv1.BookNo = 1;
        inv1.RunningNo = 100;
        await repo.SaveAsync(inv1);

        var inv2 = CreateInvoice(2566, 2);
        inv2.BookNo = 2;
        inv2.RunningNo = 250;
        await repo.SaveAsync(inv2);

        // a legacy invoice with no RunningNo must be ignored
        await repo.SaveAsync(CreateInvoice(2566, 3));

        var latest = await repo.GetLatestRunningAsync();

        Assert.NotNull(latest);
        Assert.Equal(2, latest.Value.BookNo);
        Assert.Equal(250, latest.Value.RunningNo);
    }

    [Fact]
    public async Task CountByBookNoAsync_ReturnsCountForBook()
    {
        await using var db = CreateDb();
        var repo = new AbbrInvoiceRepository(db);

        for (var i = 0; i < 3; i++)
        {
            var inv = CreateInvoice(2566, 1);
            inv.BookNo = 10;
            inv.RunningNo = 100 + i;
            await repo.SaveAsync(inv);
        }

        var inv4 = CreateInvoice(2566, 1);
        inv4.BookNo = 11;
        inv4.RunningNo = 200;
        await repo.SaveAsync(inv4);

        Assert.Equal(3, await repo.CountByBookNoAsync(10));
        Assert.Equal(1, await repo.CountByBookNoAsync(11));
        Assert.Equal(0, await repo.CountByBookNoAsync(99));
    }
}
