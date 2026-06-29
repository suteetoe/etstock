using ETStock.Data;
using ETStock.Data.Repositories;
using ETStock.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ETStock.Tests;

public class AbbrInvoiceRepositoryDeleteByPeriodTests
{
    private static AppDbContext CreateDb() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AbbrInvoice MakeInvoice(int taxYear, int taxMonth, int seq = 1) => new()
    {
        InvoiceNo = $"INV-{taxYear}{taxMonth:00}-{seq:000}",
        InvoiceDate = new DateTime(taxYear, taxMonth, 1),
        TaxYear = taxYear,
        TaxMonth = taxMonth,
        TotalAmount = 107m,
        VatAmount = 7m,
        Items =
        [
            new AbbrInvoiceItem { ProductName = "Test Product", Qty = 1m, Amount = 100m, VatAmount = 7m }
        ]
    };

    // TC-8: DeleteByPeriodAsync removes all invoices (and their items) in the given period
    [Fact]
    public async Task DeleteByPeriodAsync_RemovesAllInvoicesInPeriod()
    {
        await using var db = CreateDb();
        var repo = new AbbrInvoiceRepository(db);

        // Seed 3 invoices in 2026/6
        for (int i = 1; i <= 3; i++)
            await repo.SaveAsync(MakeInvoice(2026, 6, i));

        // Seed 2 invoices in 2026/7
        for (int i = 1; i <= 2; i++)
            await repo.SaveAsync(MakeInvoice(2026, 7, i));

        Assert.Equal(5, await db.AbbrInvoices.CountAsync());
        Assert.Equal(5, await db.AbbrInvoiceItems.CountAsync());

        await repo.DeleteByPeriodAsync(2026, 6);

        // Only the 2 invoices in 2026/7 remain
        Assert.Equal(2, await db.AbbrInvoices.CountAsync());
        Assert.All(
            await db.AbbrInvoices.ToListAsync(),
            inv => Assert.Equal(7, inv.TaxMonth));

        // No items belonging to 2026/6 invoices remain
        var remainingItems = await db.AbbrInvoiceItems.CountAsync();
        Assert.Equal(2, remainingItems);
    }

    // TC-9: DeleteByPeriodAsync on an empty DB does not throw
    [Fact]
    public async Task DeleteByPeriodAsync_NothingToDelete_DoesNotThrow()
    {
        await using var db = CreateDb();
        var repo = new AbbrInvoiceRepository(db);

        // Should complete without exception
        var ex = await Record.ExceptionAsync(() => repo.DeleteByPeriodAsync(2026, 6));

        Assert.Null(ex);
        Assert.Equal(0, await db.AbbrInvoices.CountAsync());
    }
}
