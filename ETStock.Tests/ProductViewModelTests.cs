using ETStock.Data.Repositories;
using ETStock.Models;
using ETStock.ViewModels;
using Xunit;

namespace ETStock.Tests;

public class ProductViewModelTests
{
    [Fact]
    public async Task AddProductAsync_ShowsDeterministicMessage_WhenCodeIsDuplicate()
    {
        var repository = new FakeProductRepository
        {
            AddResult = ProductWriteResult.DuplicateCode
        };
        var viewModel = CreateValidViewModel(repository);

        await viewModel.AddProductCommand.ExecuteAsync(null);

        Assert.Equal(1, repository.AddCallCount);
        Assert.Equal(ProductViewModel.DuplicateCodeMessage, viewModel.StatusMessage);
    }

    [Theory]
    [InlineData("", "Product", "piece")]
    [InlineData("P001", "   ", "piece")]
    [InlineData("P001", "Product", "\t")]
    public async Task AddProductAsync_RejectsBlankRequiredFields(
        string code,
        string name,
        string unit)
    {
        var repository = new FakeProductRepository();
        var viewModel = CreateValidViewModel(repository);
        viewModel.Code = code;
        viewModel.Name = name;
        viewModel.Unit = unit;

        await viewModel.AddProductCommand.ExecuteAsync(null);

        Assert.Equal(0, repository.AddCallCount);
        Assert.NotEmpty(viewModel.StatusMessage);
    }

    [Theory]
    [InlineData("-0.01", "1")]
    [InlineData("1", "-0.01")]
    public async Task AddProductAsync_RejectsNegativePrices(
        string costPrice,
        string sellPrice)
    {
        var repository = new FakeProductRepository();
        var viewModel = CreateValidViewModel(repository);
        viewModel.CostPrice = costPrice;
        viewModel.SellPrice = sellPrice;

        await viewModel.AddProductCommand.ExecuteAsync(null);

        Assert.Equal(0, repository.AddCallCount);
        Assert.NotEmpty(viewModel.StatusMessage);
    }

    [Fact]
    public void ClosingQty_UpdatesLive_WhenAnyQuantityChanges()
    {
        var row = new ProductStockRowViewModel(
            new ProductWithStock(1, "P001", "Product", "piece", 10, 20, null),
            _ => Task.CompletedTask);
        var closingQtyNotifications = 0;
        row.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ProductStockRowViewModel.ClosingQty))
                closingQtyNotifications++;
        };

        row.OpeningQty = 10;
        Assert.Equal(10, row.ClosingQty);

        row.BuyQty = 5;
        Assert.Equal(15, row.ClosingQty);

        row.SellFullQty = 8;
        Assert.Equal(7, row.ClosingQty);

        row.SellPosQty = 10;
        Assert.Equal(-3, row.ClosingQty);

        Assert.Equal(4, closingQtyNotifications);
    }

    private static ProductViewModel CreateValidViewModel(FakeProductRepository repository)
    {
        return new ProductViewModel(repository)
        {
            Code = "P001",
            Name = "Product",
            Unit = "piece",
            CostPrice = "10",
            SellPrice = "20"
        };
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public ProductWriteResult AddResult { get; init; } = ProductWriteResult.Success;
        public int AddCallCount { get; private set; }

        public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Product>>([]);

        public Task<IReadOnlyList<ProductWithStock>> GetAllWithMonthlyStockAsync(
            int year,
            int month,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProductWithStock>>([]);

        public Task<Product?> GetByIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult<Product?>(null);

        public Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default) =>
            Task.FromResult<Product?>(null);

        public Task<ProductWriteResult> AddAsync(
            Product product,
            CancellationToken ct = default)
        {
            AddCallCount++;
            return Task.FromResult(AddResult);
        }

        public Task<ProductWriteResult> UpdateAsync(
            Product product,
            CancellationToken ct = default) =>
            Task.FromResult(ProductWriteResult.NotFound);

        public Task<bool> DeleteAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task<MonthlyStockSnapshot> UpsertMonthlyStockAsync(
            MonthlyStockInput stock,
            CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
