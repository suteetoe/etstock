using ETStock.Data.Repositories;
using ETStock.Services;
using ETStock.ViewModels;
using Xunit;

namespace ETStock.Tests;

public class MonthlyStockViewModelTests
{
    [Fact]
    public async Task LoadCommand_LoadsSelectedPeriodIntoRows()
    {
        var repository = new FakeMonthlyStockRepository();
        repository.SetPeriod(
            2026,
            6,
            Product(
                new MonthlyStockSnapshot(10, "สินค้า A", 2026, 6, 12, 5, 2, 1)));
        var viewModel = CreateViewModel(repository);

        await viewModel.LoadCommand.ExecuteAsync(null);

        var row = Assert.Single(viewModel.Rows);
        Assert.Equal("สินค้า A", row.Name);
        Assert.Equal(12, row.OpeningQty);
        Assert.Equal(5, row.BuyQty);
        Assert.Equal(2, row.SellFullQty);
        Assert.Equal(1, row.SellPosQty);
        Assert.Equal(14, row.ClosingQty);
        Assert.Equal((2026, 6), repository.LastGetPeriod);
        Assert.Equal("Loaded 1 products for 06/2026.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task SaveCommand_SavesEditedValuesThenReloadsRows()
    {
        var repository = new FakeMonthlyStockRepository();
        repository.SetPeriod(2026, 6, Product());
        var viewModel = CreateViewModel(repository);
        await viewModel.LoadCommand.ExecuteAsync(null);

        var row = Assert.Single(viewModel.Rows);
        row.OpeningQty = 20;
        row.BuyQty = 7;
        row.SellFullQty = 4;
        row.SellPosQty = 2;

        await viewModel.SaveCommand.ExecuteAsync(null);

        var saved = Assert.Single(repository.SavedInputs);
        Assert.Equal("สินค้า A", saved.ProductName);
        Assert.Equal((2026, 6), (saved.Year, saved.Month));
        Assert.Equal(20, saved.OpeningQty);
        Assert.Equal(7, saved.BuyQty);
        Assert.Equal(4, saved.SellFullQty);
        Assert.Equal(2, saved.SellPosQty);

        var reloaded = Assert.Single(viewModel.Rows);
        Assert.Equal(21, reloaded.ClosingQty);
        Assert.Equal(2, repository.GetPeriodCallCount);
        Assert.Equal("Saved 1 monthly stock rows.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task CarryForwardCommand_PreservesCurrentTransactionsAndReloadsRows()
    {
        var repository = new FakeMonthlyStockRepository();
        repository.SetPeriod(
            2026,
            5,
            Product(
                new MonthlyStockSnapshot(9, "สินค้า A", 2026, 5, 10, 5, 2, 1)));
        repository.SetPeriod(
            2026,
            6,
            Product(
                new MonthlyStockSnapshot(10, "สินค้า A", 2026, 6, 99, 7, 3, 2)));
        var viewModel = CreateViewModel(repository);

        await viewModel.CarryForwardCommand.ExecuteAsync(null);

        Assert.Equal((2026, 6), repository.LastCarryForwardPeriod);
        var row = Assert.Single(viewModel.Rows);
        Assert.Equal(12, row.OpeningQty);
        Assert.Equal(7, row.BuyQty);
        Assert.Equal(3, row.SellFullQty);
        Assert.Equal(2, row.SellPosQty);
        Assert.Equal(14, row.ClosingQty);
        Assert.Equal(
            "Carried forward opening quantities for 1 products.",
            viewModel.StatusMessage);
    }

    private static ProductViewModel CreateViewModel(
        IMonthlyStockRepository repository) => new(repository)
    {
        SelectedYear = 2026,
        SelectedMonth = 6
    };

    // ── GenerateInvoicesCommand: seed bootstrap + post-generate refresh ─────────

    [Fact]
    public async Task Constructor_WithInvoiceGenerator_LoadsLatestRunningOnInit()
    {
        var repository = new FakeMonthlyStockRepository();
        repository.SetPeriod(2026, 6, Product());
        var generator = new FakeInvoiceGeneratorService { LatestRunning = (3, 120) };

        var vm = new ProductViewModel(repository, generator, 2026, 6);
        await WaitForAsync(() => vm.LatestRunningDisplay != string.Empty);

        Assert.False(vm.NeedsSeed);
        Assert.Equal("เล่มที่ 3 เลขที่ 00120", vm.LatestRunningDisplay);
    }

    [Fact]
    public async Task Constructor_WithInvoiceGenerator_NoLatestRunning_SetsNeedsSeed()
    {
        var repository = new FakeMonthlyStockRepository();
        repository.SetPeriod(2026, 6, Product());
        var generator = new FakeInvoiceGeneratorService { LatestRunning = null };

        var vm = new ProductViewModel(repository, generator, 2026, 6);
        await WaitForAsync(() => vm.NeedsSeed && !string.IsNullOrEmpty(vm.StatusMessage));

        Assert.True(vm.NeedsSeed);
        Assert.Contains("กรุณาระบุเล่มที่และเลขที่เริ่มต้น", vm.StatusMessage);
    }

    [Fact]
    public async Task GenerateInvoicesCommand_NoLatestRunning_SeedNotFilled_BlocksAndWarns()
    {
        var repository = new FakeMonthlyStockRepository();
        repository.SetPeriod(2026, 6, Product(
            new MonthlyStockSnapshot(10, "สินค้า A", 2026, 6, 12, 5, 2, 1)));
        var generator = new FakeInvoiceGeneratorService { LatestRunning = null };

        var vm = new ProductViewModel(repository, generator, 2026, 6);
        await WaitForAsync(() => vm.NeedsSeed);

        await vm.GenerateInvoicesCommand.ExecuteAsync(null);

        Assert.False(generator.GenerateAsyncCalled);
        Assert.Contains("กรุณาระบุเล่มที่และเลขที่เริ่มต้น", vm.StatusMessage);
    }

    [Fact]
    public async Task GenerateInvoicesCommand_NoLatestRunning_SeedFilled_PassesSeedStart()
    {
        var repository = new FakeMonthlyStockRepository();
        repository.SetPeriod(2026, 6, Product(
            new MonthlyStockSnapshot(10, "สินค้า A", 2026, 6, 12, 5, 2, 1)));
        var generator = new FakeInvoiceGeneratorService
        {
            LatestRunning = null,
            GenerateResult = new GenerateInvoicesResult(1, 107m, 7m),
        };

        var vm = new ProductViewModel(repository, generator, 2026, 6);
        await WaitForAsync(() => vm.NeedsSeed);
        vm.SeedBookNo = 1;
        vm.SeedRunningNo = 1;

        await vm.GenerateInvoicesCommand.ExecuteAsync(null);

        Assert.True(generator.GenerateAsyncCalled);
        Assert.Equal((1, 1), generator.LastSeedStart);
    }

    [Fact]
    public async Task GenerateInvoicesCommand_AfterSuccess_RefreshesLatestRunningDisplay()
    {
        var repository = new FakeMonthlyStockRepository();
        repository.SetPeriod(2026, 6, Product(
            new MonthlyStockSnapshot(10, "สินค้า A", 2026, 6, 12, 5, 2, 1)));
        var generator = new FakeInvoiceGeneratorService
        {
            LatestRunning = (1, 5),
            GenerateResult = new GenerateInvoicesResult(1, 107m, 7m),
        };

        var vm = new ProductViewModel(repository, generator, 2026, 6);
        await WaitForAsync(() => !string.IsNullOrEmpty(vm.LatestRunningDisplay));

        // Simulate the running number advancing after generation.
        generator.LatestRunning = (1, 6);

        await vm.GenerateInvoicesCommand.ExecuteAsync(null);

        Assert.Equal("เล่มที่ 1 เลขที่ 00006", vm.LatestRunningDisplay);
        Assert.False(vm.NeedsSeed);
        Assert.Contains("สร้างใบกำกับภาษีอย่างย่อ", vm.StatusMessage);
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                throw new TimeoutException("Condition not met within timeout.");
            await Task.Delay(10);
        }
    }

    private sealed class FakeInvoiceGeneratorService : IInvoiceGeneratorService
    {
        public (int BookNo, int RunningNo)? LatestRunning { get; set; }
        public GenerateInvoicesResult? GenerateResult { get; set; }
        public bool GenerateAsyncCalled { get; private set; }
        public (int BookNo, int RunningNo)? LastSeedStart { get; private set; }

        public Task<int> GetExistingCountAsync(int taxYear, int taxMonth, CancellationToken ct = default) =>
            Task.FromResult(0);

        public Task<GenerateInvoicesResult?> GenerateAsync(
            int taxYear,
            int taxMonth,
            IReadOnlyList<PosStockLine> stockLines,
            bool replaceExisting = false,
            (int BookNo, int RunningNo)? seedStart = null,
            CancellationToken ct = default)
        {
            GenerateAsyncCalled = true;
            LastSeedStart = seedStart;
            return Task.FromResult(GenerateResult);
        }

        public Task<(int BookNo, int RunningNo)?> GetLatestRunningAsync(CancellationToken ct = default) =>
            Task.FromResult(LatestRunning);
    }

    private static ProductWithStock Product(
        MonthlyStockSnapshot? stock = null) => new(
        "สินค้า A",
        "ชิ้น",
        100m,
        150m,
        stock);

    private sealed class FakeMonthlyStockRepository : IMonthlyStockRepository
    {
        private readonly Dictionary<(int Year, int Month), List<ProductWithStock>>
            _periods = [];

        public List<MonthlyStockInput> SavedInputs { get; } = [];
        public (int Year, int Month)? LastGetPeriod { get; private set; }
        public (int Year, int Month)? LastCarryForwardPeriod { get; private set; }
        public int GetPeriodCallCount { get; private set; }

        public void SetPeriod(
            int year,
            int month,
            params ProductWithStock[] products)
        {
            _periods[(year, month)] = [.. products];
        }

        public Task<IReadOnlyList<ProductWithStock>> GetPeriodAsync(
            int year,
            int month,
            CancellationToken ct = default)
        {
            LastGetPeriod = (year, month);
            GetPeriodCallCount++;
            _periods.TryGetValue((year, month), out var products);
            return Task.FromResult<IReadOnlyList<ProductWithStock>>(
                products is null ? [] : [.. products]);
        }

        public Task<MonthlyStockSnapshot> SaveAsync(
            MonthlyStockInput stock,
            CancellationToken ct = default)
        {
            SavedInputs.Add(stock);
            var snapshot = ToSnapshot(stock);
            _periods.TryGetValue((stock.Year, stock.Month), out var products);
            if (products is not null)
            {
                var index = products.FindIndex(product => product.ProductName == stock.ProductName);
                if (index >= 0)
                    products[index] = products[index] with { MonthlyStock = snapshot };
                else
                    products.Add(new ProductWithStock(stock.ProductName, stock.Unit, stock.CostPrice, stock.SellPrice, snapshot));
            }
            else
            {
                _periods[(stock.Year, stock.Month)] =
                [
                    new ProductWithStock(stock.ProductName, stock.Unit, stock.CostPrice, stock.SellPrice, snapshot)
                ];
            }
            return Task.FromResult(snapshot);
        }

        public Task<IReadOnlyList<MonthlyStockSnapshot>> CarryForwardAsync(
            int year,
            int month,
            CancellationToken ct = default)
        {
            LastCarryForwardPeriod = (year, month);
            var previous = month == 1 ? (year - 1, 12) : (year, month - 1);
            _periods.TryGetValue(previous, out var previousProducts);
            _periods.TryGetValue((year, month), out var currentProducts);

            if (previousProducts is null || currentProducts is null)
            {
                return Task.FromResult<IReadOnlyList<MonthlyStockSnapshot>>([]);
            }

            var carried = new List<MonthlyStockSnapshot>();
            for (var index = 0; index < currentProducts.Count; index++)
            {
                var current = currentProducts[index];
                var prior = previousProducts.SingleOrDefault(
                    product => product.ProductName == current.ProductName);
                if (prior?.MonthlyStock is null)
                {
                    continue;
                }

                var stock = current.MonthlyStock;
                var snapshot = new MonthlyStockSnapshot(
                    stock?.Id ?? 0,
                    current.ProductName,
                    year,
                    month,
                    prior.MonthlyStock.ClosingQty,
                    stock?.BuyQty ?? 0,
                    stock?.SellFullQty ?? 0,
                    stock?.SellPosQty ?? 0);
                currentProducts[index] = current with { MonthlyStock = snapshot };
                carried.Add(snapshot);
            }

            return Task.FromResult<IReadOnlyList<MonthlyStockSnapshot>>(carried);
        }

        public Task<bool> DeleteAsync(
            string productName,
            int year,
            int month,
            CancellationToken ct = default)
        {
            if (!_periods.TryGetValue((year, month), out var products))
                return Task.FromResult(false);
            var removed = products.RemoveAll(p => p.ProductName == productName) > 0;
            return Task.FromResult(removed);
        }

        private static MonthlyStockSnapshot ToSnapshot(
            MonthlyStockInput stock) => new(
            1,
            stock.ProductName,
            stock.Year,
            stock.Month,
            stock.OpeningQty,
            stock.BuyQty,
            stock.SellFullQty,
            stock.SellPosQty);
    }
}
