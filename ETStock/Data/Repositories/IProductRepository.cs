using ETStock.Models;

namespace ETStock.Data.Repositories;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductWithStock>> GetAllWithMonthlyStockAsync(
        int year,
        int month,
        CancellationToken ct = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<ProductWriteResult> AddAsync(Product product, CancellationToken ct = default);
    Task<ProductWriteResult> UpdateAsync(Product product, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<MonthlyStockSnapshot> UpsertMonthlyStockAsync(
        MonthlyStockInput stock,
        CancellationToken ct = default);
}
