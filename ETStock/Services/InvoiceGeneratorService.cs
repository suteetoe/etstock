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

    public Task<(int BookNo, int RunningNo)?> GetLatestRunningAsync(CancellationToken ct = default)
        => repo.GetLatestRunningAsync(ct);

    public async Task<GenerateInvoicesResult?> GenerateAsync(
        int taxYear,
        int taxMonth,
        IReadOnlyList<PosStockLine> stockLines,
        bool replaceExisting = false,
        (int BookNo, int RunningNo)? seedStart = null,
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

        // 4a. delete existing invoices for this period BEFORE computing the
        // starting running-number/book-number, so invoices about to be
        // replaced never influence the new numbering (they would otherwise
        // permanently burn running numbers / trigger phantom book rollovers
        // on every regenerate click).
        if (replaceExisting)
            await repo.DeleteByPeriodAsync(taxYear, taxMonth, ct);

        // 4b. determine the running-number / book-number starting point
        var latest = await repo.GetLatestRunningAsync(ct);

        int currentBook;
        int currentRunningNo;
        int countInCurrentBook;

        if (latest is not null)
        {
            currentBook = latest.Value.BookNo;
            currentRunningNo = latest.Value.RunningNo + 1;
            countInCurrentBook = await repo.CountByBookNoAsync(latest.Value.BookNo, ct);
        }
        else if (seedStart is not null)
        {
            currentBook = seedStart.Value.BookNo;
            currentRunningNo = seedStart.Value.RunningNo;
            countInCurrentBook = 0;
        }
        else
        {
            throw new InvalidOperationException("ไม่พบเลขที่ใบกำกับล่าสุด กรุณาระบุเล่มที่และเลขที่เริ่มต้น");
        }

        // 5. partition each product's qty across N invoice slots
        //    partitions[productIndex][invoiceIndex] = qty for that slot
        var partitions = validLines
            .Select(l => RandomPartition(l.SellPosQty, n, _rng))
            .ToList();

        // 6. build AbbrInvoice list
        var daysInMonth = DateTime.DaysInMonth(taxYear, taxMonth);
        var invoices = new List<AbbrInvoice>();
        var isFirstAssigned = true;

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

            // advance the running number for every invoice after the first one
            // assigned in this batch (the first uses the starting point as-is)
            if (!isFirstAssigned)
                currentRunningNo++;
            isFirstAssigned = false;

            countInCurrentBook++;
            if (countInCurrentBook > 50)
            {
                currentBook++;
                countInCurrentBook = 1;
            }

            var invoice = new AbbrInvoice
            {
                InvoiceNo = currentRunningNo.ToString("00000"),
                BookNo = currentBook,
                RunningNo = currentRunningNo,
                InvoiceDate = invoiceDate,
                TaxYear = taxYear,
                TaxMonth = taxMonth,
                TotalAmount = items.Sum(it => it.Amount + it.VatAmount),
                VatAmount = items.Sum(it => it.VatAmount),
                Items = items
            };

            invoices.Add(invoice);
        }

        // 7. save all invoices
        foreach (var invoice in invoices)
            await repo.SaveAsync(invoice);

        // 8. return result summary
        var totalAmount = invoices.Sum(inv => inv.TotalAmount);
        var totalVat = invoices.Sum(inv => inv.VatAmount);
        return new GenerateInvoicesResult(invoices.Count, totalAmount, totalVat);
    }

    private static decimal[] RandomPartition(decimal total, int n, Random rng)
    {
        // qty sold per invoice line must be a whole number, so partition on
        // integer cut points instead of fractional ones (any sub-1 remainder
        // of `total` is dropped rather than invoiced).
        var totalInt = (int)Math.Floor(total);
        var result = new decimal[n];

        if (totalInt <= 0)
            return result;

        if (n == 1)
        {
            result[0] = totalInt;
            return result;
        }

        var cuts = new int[n - 1];
        for (int i = 0; i < n - 1; i++)
            cuts[i] = rng.Next(0, totalInt + 1);
        Array.Sort(cuts);

        var prev = 0;
        for (int i = 0; i < n - 1; i++)
        {
            result[i] = cuts[i] - prev;
            prev = cuts[i];
        }
        result[n - 1] = totalInt - prev;
        return result;
    }
}
