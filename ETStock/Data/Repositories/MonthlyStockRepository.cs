using ETStock.Models;
using Microsoft.EntityFrameworkCore;

namespace ETStock.Data.Repositories;

public class MonthlyStockRepository(AppDbContext db) : IMonthlyStockRepository
{
    public async Task<IReadOnlyList<ProductWithStock>> GetPeriodAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        ValidatePeriod(year, month);

        return await db.MonthlyStocks
            .AsNoTracking()
            .Where(stock => stock.Year == year && stock.Month == month)
            .OrderBy(stock => stock.Id)
            .Select(stock => new ProductWithStock(
                stock.ProductName,
                stock.Unit,
                stock.CostPrice,
                stock.SellPrice,
                new MonthlyStockSnapshot(
                    stock.Id,
                    stock.ProductName,
                    stock.Year,
                    stock.Month,
                    stock.OpeningQty,
                    stock.BuyQty,
                    stock.SellFullQty,
                    stock.SellPosQty,
                    stock.CarryForwardQty)))
            .ToListAsync(ct);
    }

    public async Task<MonthlyStockSnapshot> SaveAsync(
        MonthlyStockInput stock,
        CancellationToken ct = default)
    {
        ValidatePeriod(stock.Year, stock.Month);

        var entity = await db.MonthlyStocks.SingleOrDefaultAsync(
            candidate =>
                candidate.ProductName == stock.ProductName
                && candidate.Year == stock.Year
                && candidate.Month == stock.Month,
            ct);

        if (entity is null)
        {
            entity = new MonthlyStock
            {
                ProductName = stock.ProductName,
                Unit = stock.Unit,
                CostPrice = stock.CostPrice,
                SellPrice = stock.SellPrice,
                Year = stock.Year,
                Month = stock.Month
            };
            db.MonthlyStocks.Add(entity);
        }
        else
        {
            entity.Unit = stock.Unit;
            entity.CostPrice = stock.CostPrice;
            entity.SellPrice = stock.SellPrice;
        }

        entity.OpeningQty = stock.OpeningQty;
        entity.BuyQty = stock.BuyQty;
        entity.SellFullQty = stock.SellFullQty;
        entity.SellPosQty = stock.SellPosQty;
        entity.SalesAmount = stock.SellPosQty * stock.SellPrice;
        var closingQty = stock.OpeningQty + stock.BuyQty - stock.SellFullQty - stock.SellPosQty;
        entity.ClosingValue = closingQty * stock.CostPrice;
        entity.CarryForwardQty = closingQty;

        await db.SaveChangesAsync(ct);
        return ToSnapshot(entity);
    }

    public async Task<IReadOnlyList<MonthlyStockSnapshot>> CarryForwardAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        ValidatePeriod(year, month);
        var previous = GetPreviousPeriod(year, month);

        var previousRows = await db.MonthlyStocks
            .AsNoTracking()
            .Where(stock => stock.Year == previous.Year && stock.Month == previous.Month)
            .ToListAsync(ct);

        if (previousRows.Count == 0)
        {
            return [];
        }

        var productNames = previousRows.Select(stock => stock.ProductName).ToArray();
        var currentRows = await db.MonthlyStocks
            .Where(stock =>
                stock.Year == year
                && stock.Month == month
                && productNames.Contains(stock.ProductName))
            .ToDictionaryAsync(stock => stock.ProductName, ct);

        foreach (var previousRow in previousRows)
        {
            if (!currentRows.TryGetValue(previousRow.ProductName, out var currentRow))
            {
                currentRow = new MonthlyStock
                {
                    ProductName = previousRow.ProductName,
                    Unit = previousRow.Unit,
                    CostPrice = previousRow.CostPrice,
                    SellPrice = previousRow.SellPrice,
                    Year = year,
                    Month = month
                };
                db.MonthlyStocks.Add(currentRow);
                currentRows.Add(previousRow.ProductName, currentRow);
            }
            else
            {
                // Update product details from previous row
                currentRow.Unit = previousRow.Unit;
                currentRow.CostPrice = previousRow.CostPrice;
                currentRow.SellPrice = previousRow.SellPrice;
            }

            currentRow.OpeningQty = previousRow.ClosingQty;
            currentRow.BuyQty = 0;
            currentRow.SellFullQty = 0;
            currentRow.SellPosQty = 0;
            currentRow.SalesAmount = 0;
            currentRow.ClosingValue = currentRow.OpeningQty * currentRow.CostPrice;
            currentRow.CarryForwardQty = currentRow.OpeningQty;
        }

        await db.SaveChangesAsync(ct);

        return currentRows.Values
            .OrderBy(stock => stock.ProductName)
            .Select(ToSnapshot)
            .ToList();
    }

    public async Task<bool> DeleteAsync(
        string productName,
        int year,
        int month,
        CancellationToken ct = default)
    {
        var entity = await db.MonthlyStocks.SingleOrDefaultAsync(
            stock =>
                stock.ProductName == productName
                && stock.Year == year
                && stock.Month == month,
            ct);

        if (entity is null)
            return false;

        db.MonthlyStocks.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static (int Year, int Month) GetPreviousPeriod(int year, int month)
    {
        return month == 1
            ? (year - 1, 12)
            : (year, month - 1);
    }

    private static MonthlyStockSnapshot ToSnapshot(MonthlyStock stock)
    {
        return new MonthlyStockSnapshot(
            stock.Id,
            stock.ProductName,
            stock.Year,
            stock.Month,
            stock.OpeningQty,
            stock.BuyQty,
            stock.SellFullQty,
            stock.SellPosQty,
            stock.CarryForwardQty);
    }

    private static void ValidatePeriod(int year, int month)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1);

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(
                nameof(month),
                month,
                "Month must be between 1 and 12.");
        }
    }
}
