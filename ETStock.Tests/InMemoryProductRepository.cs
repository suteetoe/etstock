using ETStock.Data.Repositories;
using ETStock.Models;

namespace ETStock.Tests;

internal sealed class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products = [];
    private int _nextId = 1;

    public IReadOnlyList<Product> Products => _products.AsReadOnly();

    public int AddCallCount { get; private set; }
    public int DeleteCallCount { get; private set; }
    public List<int> DeletedIds { get; } = [];

    public void Seed(params Product[] products)
    {
        foreach (var p in products)
        {
            if (p.Id == 0) p.Id = _nextId++;
            _products.Add(p);
        }
    }

    public Task<ProductWriteResult> AddAsync(Product product, CancellationToken ct = default)
    {
        AddCallCount++;
        if (_products.Any(p => p.Code == product.Code))
            return Task.FromResult(ProductWriteResult.DuplicateCode);

        product.Id = _nextId++;
        _products.Add(product);
        return Task.FromResult(ProductWriteResult.Success);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        DeleteCallCount++;
        DeletedIds.Add(id);
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product is null) return Task.FromResult(false);
        _products.Remove(product);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Product>>(_products.ToList());

    public Task<IReadOnlyList<ProductWithStock>> GetAllWithMonthlyStockAsync(
        int year, int month, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ProductWithStock>>([]);

    public Task<Product?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(_products.FirstOrDefault(p => p.Id == id));

    public Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        Task.FromResult(_products.FirstOrDefault(p => p.Code == code));

    public Task<ProductWriteResult> UpdateAsync(Product product, CancellationToken ct = default)
    {
        var existing = _products.FirstOrDefault(p => p.Id == product.Id);
        if (existing is null) return Task.FromResult(ProductWriteResult.NotFound);
        _products.Remove(existing);
        _products.Add(product);
        return Task.FromResult(ProductWriteResult.Success);
    }

    public Task<MonthlyStockSnapshot> UpsertMonthlyStockAsync(
        MonthlyStockInput stock, CancellationToken ct = default) =>
        Task.FromResult(new MonthlyStockSnapshot(0, stock.ProductId, stock.Year, stock.Month,
            stock.OpeningQty, stock.BuyQty, stock.SellFullQty, stock.SellPosQty));
}
