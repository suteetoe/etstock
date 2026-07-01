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

            var result = await svc.GenerateAsync(2026, 6, lines, seedStart: (1, 1));

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

            var result = await svc.GenerateAsync(2026, 6, lines, seedStart: (1, 1));

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

    // TC-3b: partitioned qty per invoice item must always be a whole number
    [Fact]
    public async Task GenerateAsync_ItemQty_AlwaysIntegers()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            var lines = new List<PosStockLine>
            {
                new("Product A", 17m, 107m),
                new("Product B", 8m, 214m)
            };

            var result = await svc.GenerateAsync(2026, 6, lines, seedStart: (1, 1));

            Assert.NotNull(result);

            var invoices = await db.AbbrInvoices
                .Include(i => i.Items)
                .Where(i => i.TaxYear == 2026 && i.TaxMonth == 6)
                .ToListAsync();

            var allItems = invoices.SelectMany(inv => inv.Items).ToList();
            Assert.NotEmpty(allItems);
            Assert.All(allItems, item => Assert.Equal(0m, item.Qty % 1m));
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

            var result = await svc.GenerateAsync(2026, 6, lines, seedStart: (1, 1));

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

            var result = await svc.GenerateAsync(2026, 6, lines, replaceExisting: true, seedStart: (1, 1));

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
            var result = await svc.GenerateAsync(2026, 6, lines, replaceExisting: false, seedStart: (1, 1));

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

    // TC-8: no prior RunningNo rows and no seedStart -> throws InvalidOperationException with exact message
    [Fact]
    public async Task GenerateAsync_NoPriorRunningNo_NoSeedStart_Throws()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            var lines = new List<PosStockLine>
            {
                new("Product A", 5m, 107m)
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.GenerateAsync(2026, 6, lines));

            Assert.Equal("ไม่พบเลขที่ใบกำกับล่าสุด กรุณาระบุเล่มที่และเลขที่เริ่มต้น", ex.Message);
        }
    }

    // TC-9: no prior RunningNo rows but seedStart given -> first invoice gets seedStart's BookNo/RunningNo as-is
    [Fact]
    public async Task GenerateAsync_NoPriorRunningNo_WithSeedStart_UsesSeedAsFirstInvoice()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            var lines = new List<PosStockLine>
            {
                new("Product A", 1m, 107m)
            };

            var result = await svc.GenerateAsync(2026, 6, lines, seedStart: (233, 11600));

            Assert.NotNull(result);

            var invoices = await db.AbbrInvoices
                .Where(i => i.TaxYear == 2026 && i.TaxMonth == 6)
                .OrderBy(i => i.RunningNo)
                .ToListAsync();

            Assert.NotEmpty(invoices);
            Assert.Equal(233, invoices[0].BookNo);
            Assert.Equal(11600, invoices[0].RunningNo);
            Assert.Equal("11600", invoices[0].InvoiceNo);
        }
    }

    // TC-10: worked example - book 232 full at 50 invoices ending RunningNo 11599 -> next 2 invoices roll to book 233
    [Fact]
    public async Task GenerateAsync_BookFull_RollsOverToNextBook()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            var repo = new AbbrInvoiceRepository(db);

            // seed 50 invoices in book 232, RunningNo 11550..11599
            for (var i = 0; i < 50; i++)
            {
                var runningNo = 11550 + i;
                var inv = MakeInvoice(2025, 1, i + 1);
                inv.BookNo = 232;
                inv.RunningNo = runningNo;
                inv.InvoiceNo = runningNo.ToString("00000");
                await repo.SaveAsync(inv);
            }

            // force exactly 2 invoices generated this run
            var lines = new List<PosStockLine>
            {
                new("Product A", 2m, 107m)
            };

            GenerateInvoicesResult? result = null;
            List<AbbrInvoice> invoices = [];
            for (var attempt = 0; attempt < 50 && (result is null || invoices.Count != 2); attempt++)
            {
                var existing = await db.AbbrInvoices
                    .Where(i => i.TaxYear == 2026 && i.TaxMonth == 6)
                    .ToListAsync();
                db.AbbrInvoices.RemoveRange(existing);
                await db.SaveChangesAsync();

                result = await svc.GenerateAsync(2026, 6, lines);

                invoices = await db.AbbrInvoices
                    .Where(i => i.TaxYear == 2026 && i.TaxMonth == 6)
                    .OrderBy(i => i.RunningNo)
                    .ToListAsync();
            }

            Assert.NotNull(result);
            Assert.Equal(2, invoices.Count);

            Assert.Equal(233, invoices[0].BookNo);
            Assert.Equal(11600, invoices[0].RunningNo);
            Assert.Equal("11600", invoices[0].InvoiceNo);

            Assert.Equal(233, invoices[1].BookNo);
            Assert.Equal(11601, invoices[1].RunningNo);
            Assert.Equal("11601", invoices[1].InvoiceNo);
        }
    }

    // TC-12: regenerating the same period (replaceExisting=true) when that period holds
    // the globally-latest invoices must NOT advance the running number / book number,
    // because the about-to-be-deleted invoices are the only contributors to "latest".
    // The delete must happen BEFORE GetLatestRunningAsync is evaluated; otherwise every
    // regenerate click would permanently burn running numbers / trigger phantom rollovers.
    [Fact]
    public async Task GenerateAsync_RegenerateSamePeriod_DoesNotAdvanceRunningNo()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            var lines = new List<PosStockLine>
            {
                new("Product A", 3m, 107m)
            };

            // First generation for period 2026/6, seeded at book 1, running 1.
            var firstResult = await svc.GenerateAsync(2026, 6, lines, seedStart: (1, 1));
            Assert.NotNull(firstResult);

            var firstInvoices = await db.AbbrInvoices
                .Where(i => i.TaxYear == 2026 && i.TaxMonth == 6)
                .OrderBy(i => i.RunningNo)
                .ToListAsync();
            Assert.NotEmpty(firstInvoices);
            var firstMinRunningNo = firstInvoices.Min(i => i.RunningNo);
            var firstMaxRunningNo = firstInvoices.Max(i => i.RunningNo);
            var firstBookNo = firstInvoices[0].BookNo;

            // Regenerate the SAME period with replaceExisting=true. Since these are the
            // only invoices in the DB, after deletion there is no "latest" left, so the
            // new batch must restart from the same seed point (book 1, running 1) rather
            // than continuing from the now-deleted invoices' running numbers.
            var secondResult = await svc.GenerateAsync(2026, 6, lines, replaceExisting: true, seedStart: (1, 1));
            Assert.NotNull(secondResult);

            var secondInvoices = await db.AbbrInvoices
                .Where(i => i.TaxYear == 2026 && i.TaxMonth == 6)
                .OrderBy(i => i.RunningNo)
                .ToListAsync();
            Assert.NotEmpty(secondInvoices);

            // Old invoices (by Id) must be gone.
            var oldIds = firstInvoices.Select(i => i.Id).ToHashSet();
            Assert.DoesNotContain(secondInvoices, i => oldIds.Contains(i.Id));

            // The regenerated batch must restart from the seed, not continue past the
            // deleted batch's running numbers. (The new batch may generate more or fewer
            // invoices than the first, so we only assert the starting point.)
            Assert.Equal(firstBookNo, secondInvoices[0].BookNo);
            Assert.Equal(firstMinRunningNo, secondInvoices[0].RunningNo);
        }
    }

    // TC-13: invoice dates must be non-decreasing as RunningNo increases
    [Fact]
    public async Task GenerateAsync_InvoiceDates_NonDecreasingWithRunningNo()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            var lines = new List<PosStockLine>
            {
                new("Product A", 20m, 107m)
            };

            List<AbbrInvoice> invoices = [];
            for (var attempt = 0; attempt < 50 && invoices.Count < 2; attempt++)
            {
                db.AbbrInvoices.RemoveRange(db.AbbrInvoices.ToList());
                await db.SaveChangesAsync();

                await svc.GenerateAsync(2026, 6, lines, seedStart: (1, 1));

                invoices = await db.AbbrInvoices
                    .Where(i => i.TaxYear == 2026 && i.TaxMonth == 6)
                    .OrderBy(i => i.RunningNo)
                    .ToListAsync();
            }

            if (invoices.Count < 2) return;

            for (int i = 0; i < invoices.Count - 1; i++)
            {
                Assert.True(
                    invoices[i].InvoiceDate <= invoices[i + 1].InvoiceDate,
                    $"RunningNo {invoices[i].RunningNo} date {invoices[i].InvoiceDate:yyyy-MM-dd} " +
                    $"is after RunningNo {invoices[i + 1].RunningNo} date {invoices[i + 1].InvoiceDate:yyyy-MM-dd}");
            }
        }
    }

    // TC-11: mid-book case - book not yet full -> no rollover, RunningNo continues in same book
    [Fact]
    public async Task GenerateAsync_BookNotFull_NoRollover()
    {
        var (svc, db) = CreateService();
        await using (db)
        {
            var repo = new AbbrInvoiceRepository(db);

            // seed 10 invoices in book 5, RunningNo 111..120
            for (var i = 0; i < 10; i++)
            {
                var runningNo = 111 + i;
                var inv = MakeInvoice(2025, 1, i + 1);
                inv.BookNo = 5;
                inv.RunningNo = runningNo;
                inv.InvoiceNo = runningNo.ToString("00000");
                await repo.SaveAsync(inv);
            }

            var lines = new List<PosStockLine>
            {
                new("Product A", 1m, 107m)
            };

            var result = await svc.GenerateAsync(2026, 6, lines);

            Assert.NotNull(result);

            var invoices = await db.AbbrInvoices
                .Where(i => i.TaxYear == 2026 && i.TaxMonth == 6)
                .OrderBy(i => i.RunningNo)
                .ToListAsync();

            Assert.NotEmpty(invoices);
            Assert.Equal(5, invoices[0].BookNo);
            Assert.Equal(121, invoices[0].RunningNo);
        }
    }
}
