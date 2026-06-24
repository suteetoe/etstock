using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Data.Repositories;

namespace ETStock.ViewModels;

public partial class ProductViewModel
{
    private readonly IMonthlyStockRepository? _repository;

    public ProductViewModel()
    {
        var today = DateTime.Today;
        _selectedYear = today.Year;
        _selectedMonth = today.Month;
    }

    public ProductViewModel(IMonthlyStockRepository repository)
        : this()
    {
        _repository = repository;
    }

    public ObservableCollection<MonthlyStockRowViewModel> Rows { get; } = [];

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private int _selectedMonth;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (!CanRun()) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            await LoadRowsAsync();
            StatusMessage = $"Loaded {Rows.Count} products for {SelectedMonth:00}/{SelectedYear}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to load monthly stock: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CarryForwardAsync()
    {
        if (!CanRun()) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var carried = await _repository!.CarryForwardAsync(SelectedYear, SelectedMonth);
            await LoadRowsAsync();
            StatusMessage = carried.Count == 0
                ? "No previous-period stock was available to carry forward."
                : $"Carried forward opening quantities for {carried.Count} products.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to carry stock forward: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanRun()) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            foreach (var row in Rows)
            {
                await _repository!.SaveAsync(row.ToInput(SelectedYear, SelectedMonth));
            }

            await LoadRowsAsync();
            StatusMessage = $"Saved {Rows.Count} monthly stock rows.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to save monthly stock: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRun()
    {
        if (_repository is null)
        {
            StatusMessage = "Monthly stock repository is not available.";
            return false;
        }

        if (SelectedYear < 1 || SelectedMonth is < 1 or > 12)
        {
            StatusMessage = "Enter a valid year and a month from 1 to 12.";
            return false;
        }

        return !IsBusy;
    }

    private async Task LoadRowsAsync()
    {
        var products = await _repository!.GetPeriodAsync(SelectedYear, SelectedMonth);

        Rows.Clear();
        foreach (var product in products)
        {
            Rows.Add(new MonthlyStockRowViewModel(product));
        }
    }
}

public partial class MonthlyStockRowViewModel : ViewModelBase
{
    public MonthlyStockRowViewModel(ProductWithStock product)
    {
        ProductId = product.Id;
        Code = product.Code;
        Name = product.Name;
        Unit = product.Unit;
        CostPrice = product.CostPrice;
        SellPrice = product.SellPrice;

        var stock = product.MonthlyStock;
        _openingQty = stock?.OpeningQty ?? 0;
        _buyQty = stock?.BuyQty ?? 0;
        _sellFullQty = stock?.SellFullQty ?? 0;
        _sellPosQty = stock?.SellPosQty ?? 0;
    }

    public int ProductId { get; }
    public string Code { get; }
    public string Name { get; }
    public string Unit { get; }
    public decimal CostPrice { get; }
    public decimal SellPrice { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingQty))]
    private decimal _openingQty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingQty))]
    private decimal _buyQty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingQty))]
    private decimal _sellFullQty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingQty))]
    private decimal _sellPosQty;

    public decimal ClosingQty => OpeningQty + BuyQty - SellFullQty - SellPosQty;

    public MonthlyStockInput ToInput(int year, int month) => new(
        ProductId,
        year,
        month,
        OpeningQty,
        BuyQty,
        SellFullQty,
        SellPosQty);
}
