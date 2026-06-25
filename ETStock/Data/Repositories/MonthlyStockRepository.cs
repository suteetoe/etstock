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

        return await db.Products
            .AsNoTracking()
            .OrderBy(product => product.Code)
            .Select(product => new ProductWithStock(
                product.Id,
                product.Code,
                product.Name,
                product.Unit,
                product.CostPrice,
                product.SellPrice,
                product.MonthlyStocks
                    .Where(stock => stock.Year == year && stock.Month == month)
                    .Select(stock => new MonthlyStockSnapshot(
                        stock.Id,
                        stock.ProductId,
                        stock.Year,
                        stock.Month,
                        stock.OpeningQty,
                        stock.BuyQty,
                        stock.SellFullQty,
                        stock.SellPosQty))
                    .SingleOrDefault()))
            .ToListAsync(ct);
    }

    public async Task<MonthlyStockSnapshot> SaveAsync(
        MonthlyStockInput stock,
        CancellationToken ct = default)
    {
        ValidatePeriod(stock.Year, stock.Month);
        await EnsureProductExistsAsync(stock.ProductId, ct);

        var entity = await db.MonthlyStocks.SingleOrDefaultAsync(
            candidate =>
                candidate.ProductId == stock.ProductId
                && candidate.Year == stock.Year
                && candidate.Month == stock.Month,
            ct);

        if (entity is null)
        {
            entity = new MonthlyStock
            {
                ProductId = stock.ProductId,
                Year = stock.Year,
                Month = stock.Month
            };
            db.MonthlyStocks.Add(entity);
        }

        entity.OpeningQty = stock.OpeningQty;
        entity.BuyQty = stock.BuyQty;
        entity.SellFullQty = stock.SellFullQty;
        entity.SellPosQty = stock.SellPosQty;

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

        var productIds = previousRows.Select(stock => stock.ProductId).ToArray();
        var currentRows = await db.MonthlyStocks
            .Where(stock =>
                stock.Year == year
                && stock.Month == month
                && productIds.Contains(stock.ProductId))
            .ToDictionaryAsync(stock => stock.ProductId, ct);

        foreach (var previousRow in previousRows)
        {
            if (!currentRows.TryGetValue(previousRow.ProductId, out var currentRow))
            {
                currentRow = new MonthlyStock
                {
                    ProductId = previousRow.ProductId,
                    Year = year,
                    Month = month
                };
                db.MonthlyStocks.Add(currentRow);
                currentRows.Add(previousRow.ProductId, currentRow);
            }

            currentRow.OpeningQty = previousRow.ClosingQty;
        }

        await db.SaveChangesAsync(ct);

        return currentRows.Values
            .OrderBy(stock => stock.ProductId)
            .Select(ToSnapshot)
            .ToList();
    }

    private async Task EnsureProductExistsAsync(int productId, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(product => product.Id == productId, ct))
        {
            throw new ArgumentException(
                $"Product id {productId} does not exist.",
                nameof(productId));
        }
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
            stock.ProductId,
            stock.Year,
            stock.Month,
            stock.OpeningQty,
            stock.BuyQty,
            stock.SellFullQty,
            stock.SellPosQty);
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
