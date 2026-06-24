namespace ETStock.Data.Repositories;

public interface IMonthlyStockRepository
{
    Task<IReadOnlyList<ProductWithStock>> GetPeriodAsync(
        int year,
        int month,
        CancellationToken ct = default);

    Task<MonthlyStockSnapshot> SaveAsync(
        MonthlyStockInput stock,
        CancellationToken ct = default);

    Task<IReadOnlyList<MonthlyStockSnapshot>> CarryForwardAsync(
        int year,
        int month,
        CancellationToken ct = default);
}
