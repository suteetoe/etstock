using ETStock.Models;
using Microsoft.EntityFrameworkCore;

namespace ETStock.Data.Repositories;

public class AbbrInvoiceRepository(AppDbContext db) : IAbbrInvoiceRepository
{
    public async Task<List<AbbrInvoice>> GetByPeriodAsync(int taxYear, int taxMonth)
    {
        return await db.AbbrInvoices
            .AsNoTracking()
            .Include(i => i.Items)
            .Where(i => i.TaxYear == taxYear && i.TaxMonth == taxMonth)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<AbbrInvoice?> GetByIdAsync(int id)
    {
        return await db.AbbrInvoices
            .AsNoTracking()
            .Include(i => i.Items)
            .SingleOrDefaultAsync(i => i.Id == id);
    }

    public async Task SaveAsync(AbbrInvoice invoice)
    {
        if (invoice.Id == 0)
        {
            db.AbbrInvoices.Add(invoice);
            await db.SaveChangesAsync();
        }
        else
        {
            var existing = await db.AbbrInvoices
                .Include(i => i.Items)
                .SingleOrDefaultAsync(i => i.Id == invoice.Id);

            if (existing is null)
            {
                db.AbbrInvoices.Add(invoice);
                await db.SaveChangesAsync();
                return;
            }

            existing.InvoiceNo = invoice.InvoiceNo;
            existing.InvoiceDate = invoice.InvoiceDate;
            existing.TaxYear = invoice.TaxYear;
            existing.TaxMonth = invoice.TaxMonth;
            existing.TotalAmount = invoice.TotalAmount;
            existing.VatAmount = invoice.VatAmount;

            db.AbbrInvoiceItems.RemoveRange(existing.Items);
            foreach (var item in invoice.Items)
            {
                item.AbbrInvoiceId = existing.Id;
                db.AbbrInvoiceItems.Add(item);
            }

            await db.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var invoice = await db.AbbrInvoices
            .Include(i => i.Items)
            .SingleOrDefaultAsync(i => i.Id == id);

        if (invoice is null)
        {
            return;
        }

        db.AbbrInvoices.Remove(invoice);
        await db.SaveChangesAsync();
    }

    public async Task<AbbrInvoiceSummary> GetPeriodSummaryAsync(int taxYear, int taxMonth)
    {
        var query = db.AbbrInvoices
            .AsNoTracking()
            .Where(i => i.TaxYear == taxYear && i.TaxMonth == taxMonth);

        var count = await query.CountAsync();
        if (count == 0)
            return new AbbrInvoiceSummary(0, 0, 0);

        var totalAmount = await query.SumAsync(i => i.TotalAmount);
        var vatAmount = await query.SumAsync(i => i.VatAmount);

        return new AbbrInvoiceSummary(count, totalAmount, vatAmount);
    }
}
