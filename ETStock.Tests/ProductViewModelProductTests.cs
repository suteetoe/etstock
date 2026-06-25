using ETStock.Data.Repositories;
using ETStock.Models;
using ETStock.ViewModels;
using Xunit;

namespace ETStock.Tests;

public class ProductViewModelProductTests
{
    [Fact]
    public async Task AddProductCommand_WhenCompleted_AddsProductAndReloadsRows()
    {
        var productRepo = new InMemoryProductRepository();
        var stockRepo = new ProductAwareMonthlyStockRepository(productRepo, 2026, 6);
        var vm = CreateViewModel(stockRepo, productRepo);

        var newProduct = MakeProduct("P001", "Widget");

        vm.AddProductRequested += (_, _) => vm.CompleteAddProduct(new AddProductResult(newProduct, 0));

        await vm.AddProductCommand.ExecuteAsync(null);

        var row = Assert.Single(vm.Rows);
        Assert.Equal("P001", row.Code);
        Assert.Equal("Widget", row.Name);
        Assert.Equal(1, productRepo.AddCallCount);
    }

    [Fact]
    public async Task AddProductCommand_WhenCancelled_DoesNotAddProduct()
    {
        var productRepo = new InMemoryProductRepository();
        var stockRepo = new ProductAwareMonthlyStockRepository(productRepo, 2026, 6);
        var vm = CreateViewModel(stockRepo, productRepo);

        vm.AddProductRequested += (_, _) => vm.CompleteAddProduct(null);

        await vm.AddProductCommand.ExecuteAsync(null);

        Assert.Empty(vm.Rows);
        Assert.Equal(0, productRepo.AddCallCount);
    }

    [Fact]
    public async Task AddProductCommand_WhenRepositoryUnavailable_SetsStatusMessage()
    {
        var stockRepo = new FakeMonthlyStockRepository();
        var vm = new ProductViewModel(stockRepo)
        {
            SelectedYear = 2026,
            SelectedMonth = 6
        };

        await vm.AddProductCommand.ExecuteAsync(null);

        Assert.Contains("not available", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteProductCommand_WhenRowSelected_DeletesAndReloads()
    {
        var productRepo = new InMemoryProductRepository();
        var existingProduct = MakeProduct("P001", "Sprocket");
        productRepo.Seed(existingProduct);

        var stockRepo = new ProductAwareMonthlyStockRepository(productRepo, 2026, 6);
        stockRepo.AddSnapshot(existingProduct.Id, new MonthlyStockSnapshot(1, existingProduct.Id, 2026, 6, 0, 0, 0, 0));

        var vm = CreateViewModel(stockRepo, productRepo);
        await vm.LoadCommand.ExecuteAsync(null);

        var row = Assert.Single(vm.Rows);
        vm.SelectedRow = row;

        await vm.DeleteProductCommand.ExecuteAsync(null);

        Assert.Empty(vm.Rows);
        Assert.Equal(1, productRepo.DeleteCallCount);
        Assert.Contains(existingProduct.Id, productRepo.DeletedIds);
        Assert.Contains("Sprocket", vm.StatusMessage);
    }

    [Fact]
    public async Task DeleteProductCommand_WhenNoRowSelected_SetsStatusMessage()
    {
        var productRepo = new InMemoryProductRepository();
        var stockRepo = new ProductAwareMonthlyStockRepository(productRepo, 2026, 6);
        var vm = CreateViewModel(stockRepo, productRepo);

        await vm.DeleteProductCommand.ExecuteAsync(null);

        Assert.False(string.IsNullOrWhiteSpace(vm.StatusMessage));
        Assert.Equal(0, productRepo.DeleteCallCount);
    }

    [Fact]
    public async Task DeleteProductCommand_WhenRepositoryUnavailable_SetsStatusMessage()
    {
        var stockRepo = new FakeMonthlyStockRepository();
        var vm = new ProductViewModel(stockRepo)
        {
            SelectedYear = 2026,
            SelectedMonth = 6
        };

        await vm.DeleteProductCommand.ExecuteAsync(null);

        Assert.Contains("not available", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddProductCommand_FiresAddProductRequested()
    {
        var productRepo = new InMemoryProductRepository();
        var stockRepo = new ProductAwareMonthlyStockRepository(productRepo, 2026, 6);
        var vm = CreateViewModel(stockRepo, productRepo);

        var eventFired = false;
        vm.AddProductRequested += (_, _) =>
        {
            eventFired = true;
            vm.CompleteAddProduct(null);
        };

        await vm.AddProductCommand.ExecuteAsync(null);

        Assert.True(eventFired);
    }

    [Fact]
    public async Task AddProductCommand_WhenCompleted_StatusMessageContainsProductName()
    {
        var productRepo = new InMemoryProductRepository();
        var stockRepo = new ProductAwareMonthlyStockRepository(productRepo, 2026, 6);
        var vm = CreateViewModel(stockRepo, productRepo);

        var newProduct = MakeProduct("P042", "Flux Capacitor");
        vm.AddProductRequested += (_, _) => vm.CompleteAddProduct(new AddProductResult(newProduct, 0));

        await vm.AddProductCommand.ExecuteAsync(null);

        Assert.Contains("Flux Capacitor", vm.StatusMessage);
    }

    [Fact]
    public async Task DeleteProductCommand_WhenRowSelected_StatusMessageContainsProductName()
    {
        var productRepo = new InMemoryProductRepository();
        var existingProduct = MakeProduct("P002", "Gizmo");
        productRepo.Seed(existingProduct);

        var stockRepo = new ProductAwareMonthlyStockRepository(productRepo, 2026, 6);
        stockRepo.AddSnapshot(existingProduct.Id, new MonthlyStockSnapshot(1, existingProduct.Id, 2026, 6, 0, 0, 0, 0));

        var vm = CreateViewModel(stockRepo, productRepo);
        await vm.LoadCommand.ExecuteAsync(null);

        vm.SelectedRow = Assert.Single(vm.Rows);
        await vm.DeleteProductCommand.ExecuteAsync(null);

        Assert.Contains("Gizmo", vm.StatusMessage);
    }

    [Fact]
    public async Task AddProductCommand_WhenCompleted_RowCountMatchesReloadedProducts()
    {
        var productRepo = new InMemoryProductRepository();
        var existing = MakeProduct("P001", "Existing");
        productRepo.Seed(existing);

        var stockRepo = new ProductAwareMonthlyStockRepository(productRepo, 2026, 6);
        stockRepo.AddSnapshot(existing.Id, new MonthlyStockSnapshot(1, existing.Id, 2026, 6, 5, 0, 0, 0));

        var vm = CreateViewModel(stockRepo, productRepo);
        await vm.LoadCommand.ExecuteAsync(null);
        Assert.Single(vm.Rows);

        var newProduct = MakeProduct("P002", "New Widget");
        vm.AddProductRequested += (_, _) => vm.CompleteAddProduct(new AddProductResult(newProduct, 0));

        await vm.AddProductCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Rows.Count);
    }

    [Fact]
    public async Task AddProductCommand_WithBalanceQty_SetsOpeningQtyOnRow()
    {
        var productRepo = new InMemoryProductRepository();
        var stockRepo = new ProductAwareMonthlyStockRepository(productRepo, 2026, 6);
        var vm = CreateViewModel(stockRepo, productRepo);

        var newProduct = MakeProduct("P001", "Widget");
        vm.AddProductRequested += (_, _) => vm.CompleteAddProduct(new AddProductResult(newProduct, 50m));

        await vm.AddProductCommand.ExecuteAsync(null);

        var row = Assert.Single(vm.Rows);
        Assert.Equal(50m, row.OpeningQty);
        Assert.Equal(50m, row.ClosingQty);
    }

    [Fact]
    public async Task AddProductCommand_WithDuplicateCode_SetsErrorStatusMessage()
    {
        var productRepo = new InMemoryProductRepository();
        productRepo.Seed(MakeProduct("P001", "Existing"));
        var stockRepo = new ProductAwareMonthlyStockRepository(productRepo, 2026, 6);
        var vm = CreateViewModel(stockRepo, productRepo);

        var duplicate = MakeProduct("P001", "Duplicate");
        vm.AddProductRequested += (_, _) => vm.CompleteAddProduct(new AddProductResult(duplicate, 0));

        await vm.AddProductCommand.ExecuteAsync(null);

        Assert.Equal(1, productRepo.AddCallCount);
    }

    private static ProductViewModel CreateViewModel(
        IMonthlyStockRepository stockRepo,
        IProductRepository productRepo) => new(stockRepo, productRepo)
    {
        SelectedYear = 2026,
        SelectedMonth = 6
    };

    private static Product MakeProduct(string code, string name) => new()
    {
        Code = code,
        Name = name,
        Unit = "piece",
        CostPrice = 10m,
        SellPrice = 20m
    };

    private sealed class FakeMonthlyStockRepository : IMonthlyStockRepository
    {
        public Task<IReadOnlyList<ProductWithStock>> GetPeriodAsync(
            int year, int month, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProductWithStock>>([]);

        public Task<MonthlyStockSnapshot> SaveAsync(
            MonthlyStockInput stock, CancellationToken ct = default) =>
            Task.FromResult(new MonthlyStockSnapshot(0, stock.ProductId, stock.Year, stock.Month,
                stock.OpeningQty, stock.BuyQty, stock.SellFullQty, stock.SellPosQty));

        public Task<IReadOnlyList<MonthlyStockSnapshot>> CarryForwardAsync(
            int year, int month, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MonthlyStockSnapshot>>([]);
    }

    private sealed class ProductAwareMonthlyStockRepository(
        InMemoryProductRepository productRepo,
        int year,
        int month) : IMonthlyStockRepository
    {
        private readonly Dictionary<int, MonthlyStockSnapshot> _snapshots = [];

        public void AddSnapshot(int productId, MonthlyStockSnapshot snapshot) =>
            _snapshots[productId] = snapshot;

        public Task<IReadOnlyList<ProductWithStock>> GetPeriodAsync(
            int year2, int month2, CancellationToken ct = default)
        {
            if (year2 != year || month2 != month)
                return Task.FromResult<IReadOnlyList<ProductWithStock>>([]);

            var rows = productRepo.Products.Select(p =>
            {
                _snapshots.TryGetValue(p.Id, out var snap);
                return new ProductWithStock(p.Id, p.Code, p.Name, p.Unit, p.CostPrice, p.SellPrice, snap);
            }).ToList();

            return Task.FromResult<IReadOnlyList<ProductWithStock>>(rows);
        }

        public Task<MonthlyStockSnapshot> SaveAsync(
            MonthlyStockInput stock, CancellationToken ct = default) =>
            Task.FromResult(new MonthlyStockSnapshot(0, stock.ProductId, stock.Year, stock.Month,
                stock.OpeningQty, stock.BuyQty, stock.SellFullQty, stock.SellPosQty));

        public Task<IReadOnlyList<MonthlyStockSnapshot>> CarryForwardAsync(
            int y, int m, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MonthlyStockSnapshot>>([]);
    }
}
