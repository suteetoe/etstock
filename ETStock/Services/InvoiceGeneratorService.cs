using ETStock.Data.Repositories;
using ETStock.Models;

namespace ETStock.Services;

public class InvoiceGeneratorService(IAbbrInvoiceRepository repo) : IInvoiceGeneratorService
{
    private readonly Random _rng = new();

    public async Task<int> GetExistingCountAsync(int taxYear, int taxMonth, CancellationToken ct = default)
    {
        var list = await repo.GetByPeriodAsync(taxYear, taxMonth);
        return list.Count;
    }

    public async Task<GenerateInvoicesResult?> GenerateAsync(
        int taxYear,
        int taxMonth,
        IReadOnlyList<PosStockLine> stockLines,
        bool replaceExisting = false,
        CancellationToken ct = default)
    {
        // 1. filter stockLines that have SellPosQty > 0
        var validLines = stockLines.Where(l => l.SellPosQty > 0).ToList();

        // 2. return null if no valid lines
        if (validLines.Count == 0)
            return null;

        // 3. totalQty = sum of all SellPosQty
        var totalQty = validLines.Sum(l => l.SellPosQty);

        // 4. determine N = random number of invoices in [1, maxInvoices]
        var maxInvoices = Math.Min(20, Math.Max(1, (int)Math.Ceiling(totalQty)));
        var n = _rng.Next(1, maxInvoices + 1);

        // 5. partition each product's qty across N invoice slots
        //    partitions[productIndex][invoiceIndex] = qty for that slot
        var partitions = validLines
            .Select(l => RandomPartition(l.SellPosQty, n, _rng))
            .ToList();

        // 6. build AbbrInvoice list
        var daysInMonth = DateTime.DaysInMonth(taxYear, taxMonth);
        var invoices = new List<AbbrInvoice>();
        var seq = 1;

        for (int i = 0; i < n; i++)
        {
            var items = new List<AbbrInvoiceItem>();

            for (int p = 0; p < validLines.Count; p++)
            {
                var qty = partitions[p][i];
                if (qty <= 0m)
                    continue;

                var sellPrice = validLines[p].SellPrice;
                var amount = Math.Round(qty * sellPrice / 1.07m, 2);
                var vatAmount = Math.Round(qty * sellPrice - amount, 2);

                items.Add(new AbbrInvoiceItem
                {
                    ProductName = validLines[p].ProductName,
                    Qty = qty,
                    Amount = amount,
                    VatAmount = vatAmount
                });
            }

            // skip if no items in this slot
            if (items.Count == 0)
                continue;

            var day = _rng.Next(1, daysInMonth + 1);
            var invoiceDate = new DateTime(taxYear, taxMonth, day);

            var invoice = new AbbrInvoice
            {
                InvoiceNo = $"ABB-{taxYear}{taxMonth:00}-{seq:000}",
                InvoiceDate = invoiceDate,
                TaxYear = taxYear,
                TaxMonth = taxMonth,
                TotalAmount = items.Sum(it => it.Amount + it.VatAmount),
                VatAmount = items.Sum(it => it.VatAmount),
                Items = items
            };

            invoices.Add(invoice);
            seq++;
        }

        // 7. delete existing invoices if replaceExisting
        if (replaceExisting)
            await repo.DeleteByPeriodAsync(taxYear, taxMonth, ct);

        // 8. save all invoices
        foreach (var invoice in invoices)
            await repo.SaveAsync(invoice);

        // 9. return result summary
        var totalAmount = invoices.Sum(inv => inv.TotalAmount);
        var totalVat = invoices.Sum(inv => inv.VatAmount);
        return new GenerateInvoicesResult(invoices.Count, totalAmount, totalVat);
    }

    private static decimal[] RandomPartition(decimal total, int n, Random rng)
    {
        if (n == 1) return [total];

        var cuts = new decimal[n - 1];
        for (int i = 0; i < n - 1; i++)
            cuts[i] = Math.Round((decimal)(rng.NextDouble() * (double)total), 2);
        Array.Sort(cuts);

        var result = new decimal[n];
        decimal prev = 0;
        for (int i = 0; i < n - 1; i++)
        {
            result[i] = cuts[i] - prev;
            prev = cuts[i];
        }
        result[n - 1] = Math.Round(total - prev, 2);
        return result;
    }
}
