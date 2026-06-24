using ETStock.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ETStock.Data.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .OrderBy(product => product.Code)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProductWithStock>> GetAllWithMonthlyStockAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        ValidatePeriod(year, month);

        return await _db.Products
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

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == id, ct);
    }

    public async Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Code == code, ct);
    }

    public async Task<ProductWriteResult> AddAsync(
        Product product,
        CancellationToken ct = default)
    {
        if (await CodeExistsAsync(product.Code, excludedProductId: null, ct))
        {
            return ProductWriteResult.DuplicateCode;
        }

        _db.Products.Add(product);

        try
        {
            await _db.SaveChangesAsync(ct);
            return ProductWriteResult.Success;
        }
        catch (DbUpdateException exception) when (IsProductCodeUniqueViolation(exception))
        {
            _db.Entry(product).State = EntityState.Detached;
            return ProductWriteResult.DuplicateCode;
        }
    }

    public async Task<ProductWriteResult> UpdateAsync(
        Product product,
        CancellationToken ct = default)
    {
        var existing = await _db.Products
            .FirstOrDefaultAsync(candidate => candidate.Id == product.Id, ct);

        if (existing is null)
        {
            return ProductWriteResult.NotFound;
        }

        if (await CodeExistsAsync(product.Code, product.Id, ct))
        {
            return ProductWriteResult.DuplicateCode;
        }

        existing.Code = product.Code;
        existing.Name = product.Name;
        existing.Unit = product.Unit;
        existing.CostPrice = product.CostPrice;
        existing.SellPrice = product.SellPrice;

        try
        {
            await _db.SaveChangesAsync(ct);
            return ProductWriteResult.Success;
        }
        catch (DbUpdateException exception) when (IsProductCodeUniqueViolation(exception))
        {
            await _db.Entry(existing).ReloadAsync(ct);
            return ProductWriteResult.DuplicateCode;
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(candidate => candidate.Id == id, ct);

        if (product is null)
        {
            return false;
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<MonthlyStockSnapshot> UpsertMonthlyStockAsync(
        MonthlyStockInput stock,
        CancellationToken ct = default)
    {
        ValidatePeriod(stock.Year, stock.Month);

        if (!await _db.Products.AnyAsync(product => product.Id == stock.ProductId, ct))
        {
            throw new ArgumentException(
                $"Product id {stock.ProductId} does not exist.",
                nameof(stock));
        }

        var existing = await _db.MonthlyStocks
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.ProductId == stock.ProductId
                    && candidate.Year == stock.Year
                    && candidate.Month == stock.Month,
                ct);

        if (existing is null)
        {
            existing = new MonthlyStock
            {
                ProductId = stock.ProductId,
                Year = stock.Year,
                Month = stock.Month
            };
            _db.MonthlyStocks.Add(existing);
        }

        existing.OpeningQty = stock.OpeningQty;
        existing.BuyQty = stock.BuyQty;
        existing.SellFullQty = stock.SellFullQty;
        existing.SellPosQty = stock.SellPosQty;

        await _db.SaveChangesAsync(ct);

        return ToSnapshot(existing);
    }

    private Task<bool> CodeExistsAsync(
        string code,
        int? excludedProductId,
        CancellationToken ct)
    {
        return _db.Products.AnyAsync(
            product =>
                product.Code == code
                && (!excludedProductId.HasValue || product.Id != excludedProductId.Value),
            ct);
    }

    private static bool IsProductCodeUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Products_Code"
        };
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
