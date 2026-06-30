using ETStock.Data;
using ETStock.Data.Repositories;
using ETStock.Models;
using ETStock.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ETStock.Tests;

public class InvoiceGeneratorServiceTests
{
    private static AppDbContext CreateDb() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (InvoiceGeneratorService svc, AppDbContext db) CreateService()
    {
        var db = CreateDb();
        var repo = new AbbrInvoiceRepository(db);
        var svc = new InvoiceGeneratorService(repo);
        return (svc, db);
    }

    private static AbbrInvoice MakeInvoice(int taxYear, int taxMonth, int seq = 1) => new()
    {
        InvoiceNo = $"SEED-{taxYear}{taxMonth:00}-{seq:000}",
        InvoiceDate = new DateTime(taxYear, taxMonth, 1),
        TaxYear = taxYear,
        TaxMonth = taxMonth,
        TotalAmount = 107m,
        VatAmount = 7m,
        Items =
        [
            new AbbrInvoiceItem { ProductName = "Seed Product", Qty = 1m, Amount = 100m, VatAmount = 7m }
        ]
    };

    // TC-1: returns null when all SellPosQty = 0
    [Fact]
    public async Task GenerateAsync_NoPosSales_ReturnsNull()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            var lines = new List<PosStockLine>
            {
                new("Product A", 0m, 107m),
                new("Product B", 0m, 214m)
            };

            var result = await svc.GenerateAsync(2026, 6, lines);

            Assert.Null(result);
        }
    }

    // TC-2: single product — VAT calculation must be correct
    [Fact]
    public async Task GenerateAsync_SingleProduct_CreatesInvoices()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            var lines = new List<PosStockLine>
            {
                new("Product A", 5m, 107m)
            };

            var result = await svc.GenerateAsync(2026, 6, lines);

            Assert.NotNull(result);
            Assert.True(result.InvoiceCount >= 1);

            var invoices = await db.AbbrInvoices
                .Include(i => i.Items)
                .Where(i => i.TaxYear == 2026 && i.TaxMonth == 6)
                .ToListAsync();

            // All invoices are for the correct period
            Assert.All(invoices, inv =>
            {
                Assert.Equal(2026, inv.TaxYear);
                Assert.Equal(6, inv.TaxMonth);
            });

            // Total qty across all invoice items for "Product A" must sum to 5 (+-0.10)
            var totalQty = invoices.SelectMany(inv => inv.Items)
                .Where(it => it.ProductName == "Product A")
                .Sum(it => it.Qty);
            Assert.InRange(totalQty, 4.90m, 5.10m);

            // VAT check: for each item, Amount ~= round(qty * 107 / 1.07, 2)
            //            and VatAmount ~= qty * 107 - Amount
            var allItems = invoices.SelectMany(inv => inv.Items).ToList();
            foreach (var item in allItems)
            {
                var expectedAmount = Math.Round(item.Qty * 107m / 1.07m, 2);
                var expectedVat = Math.Round(item.Qty * 107m - expectedAmount, 2);

                Assert.InRange(item.Amount, expectedAmount - 0.02m, expectedAmount + 0.02m);
                Assert.InRange(item.VatAmount, expectedVat - 0.02m, expectedVat + 0.02m);
            }
        }
    }

    // TC-3: multiple products — each product's qty is fully distributed across invoices
    [Fact]
    public async Task GenerateAsync_MultipleProducts_AllQtyDistributed()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            var lines = new List<PosStockLine>
            {
                new("Product A", 3m, 214m),
                new("Product B", 3m, 214m),
                new("Product C", 3m, 214m)
            };

            var result = await svc.GenerateAsync(2026, 6, lines);

            Assert.NotNull(result);
            Assert.True(result.InvoiceCount >= 1);

            var invoices = await db.AbbrInvoices
                .Include(i => i.Items)
                .Where(i => i.TaxYear == 2026 && i.TaxMonth == 6)
                .ToListAsync();

            // Each product's total qty in all invoice items ~= 3 (+-0.10)
            foreach (var productName in new[] { "Product A", "Product B", "Product C" })
            {
                var qtySum = invoices.SelectMany(inv => inv.Items)
                    .Where(it => it.ProductName == productName)
                    .Sum(it => it.Qty);
                Assert.InRange(qtySum, 2.90m, 3.10m);
            }
        }
    }

    // TC-4: invoices and items are persisted to DB
    [Fact]
    public async Task GenerateAsync_SavesInvoicesToDb()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            var lines = new List<PosStockLine>
            {
                new("Product A", 2m, 107m),
                new("Product B", 3m, 214m)
            };

            var result = await svc.GenerateAsync(2026, 6, lines);

            Assert.NotNull(result);
            Assert.True(await db.AbbrInvoices.CountAsync() > 0);
            Assert.True(await db.AbbrInvoiceItems.CountAsync() > 0);
        }
    }

    // TC-5: replaceExisting=true removes old invoices and saves new ones
    [Fact]
    public async Task GenerateAsync_ReplaceExisting_DeletesOldInvoices()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            // Seed 3 old invoices
            var repo = new AbbrInvoiceRepository(db);
            var oldIds = new List<int>();
            for (int i = 1; i <= 3; i++)
            {
                var inv = MakeInvoice(2026, 6, i);
                await repo.SaveAsync(inv);
                oldIds.Add(inv.Id);
            }

            Assert.Equal(3, await db.AbbrInvoices.CountAsync());

            var lines = new List<PosStockLine>
            {
                new("Product A", 4m, 107m)
            };

            var result = await svc.GenerateAsync(2026, 6, lines, replaceExisting: true);

            Assert.NotNull(result);

            // Old invoice IDs must no longer exist
            foreach (var oldId in oldIds)
            {
                Assert.False(await db.AbbrInvoices.AnyAsync(i => i.Id == oldId));
            }

            // New invoices must exist
            var newInvoices = await db.AbbrInvoices
                .Where(i => i.TaxYear == 2026 && i.TaxMonth == 6)
                .ToListAsync();
            Assert.True(newInvoices.Count > 0);
        }
    }

    // TC-6: replaceExisting=false keeps old invoices alongside new ones
    [Fact]
    public async Task GenerateAsync_ReplaceExisting_False_KeepsOldInvoices()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            // Seed 1 old invoice
            var repo = new AbbrInvoiceRepository(db);
            var oldInv = MakeInvoice(2026, 6, 1);
            await repo.SaveAsync(oldInv);
            var oldId = oldInv.Id;

            Assert.Equal(1, await db.AbbrInvoices.CountAsync());

            var lines = new List<PosStockLine>
            {
                new("Product A", 2m, 107m)
            };

            // replaceExisting defaults to false
            var result = await svc.GenerateAsync(2026, 6, lines, replaceExisting: false);

            Assert.NotNull(result);

            // Old invoice must still be present
            Assert.True(await db.AbbrInvoices.AnyAsync(i => i.Id == oldId));

            // Total count must be > 1 (old + new)
            var totalCount = await db.AbbrInvoices.CountAsync();
            Assert.True(totalCount > 1);
        }
    }

    // TC-7: GetExistingCountAsync returns count scoped to the given period
    [Fact]
    public async Task GetExistingCountAsync_ReturnsCorrectCount()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            var repo = new AbbrInvoiceRepository(db);

            // 2 invoices in 2026/6
            await repo.SaveAsync(MakeInvoice(2026, 6, 1));
            await repo.SaveAsync(MakeInvoice(2026, 6, 2));

            // 1 invoice in 2026/7
            await repo.SaveAsync(MakeInvoice(2026, 7, 1));

            var count6 = await svc.GetExistingCountAsync(2026, 6);
            var count7 = await svc.GetExistingCountAsync(2026, 7);
            var count8 = await svc.GetExistingCountAsync(2026, 8);

            Assert.Equal(2, count6);
            Assert.Equal(1, count7);
            Assert.Equal(0, count8);
        }
    }
}
