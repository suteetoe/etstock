using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Data.Repositories;
using ETStock.Models;

namespace ETStock.ViewModels;

public partial class ProductViewModel
{
    private readonly IMonthlyStockRepository? _repository;
    private IProductRepository? _productRepository;

    public event EventHandler? AddProductRequested;

    private TaskCompletionSource<Product?>? _addProductTcs;

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

    public ProductViewModel(IMonthlyStockRepository repository, IProductRepository productRepository)
        : this(repository)
    {
        _productRepository = productRepository;
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

    [ObservableProperty]
    private MonthlyStockRowViewModel? _selectedRow;

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

    [RelayCommand]
    private async Task AddProductAsync()
    {
        if (_productRepository is null) { StatusMessage = "Product repository not available."; return; }
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        _addProductTcs = new TaskCompletionSource<Product?>();
        AddProductRequested?.Invoke(this, EventArgs.Empty);
        var newProduct = await _addProductTcs.Task;
        _addProductTcs = null;
        try
        {
            if (newProduct is not null)
            {
                await _productRepository.AddAsync(newProduct);
                var newRow = new MonthlyStockRowViewModel(new ProductWithStock(
                    newProduct.Id, newProduct.Code, newProduct.Name, newProduct.Unit,
                    newProduct.CostPrice, newProduct.SellPrice, null));
                Rows.Add(newRow);
                newRow.LineNumber = Rows.Count;
                StatusMessage = $"เพิ่มสินค้า '{newProduct.Name}' เรียบร้อยแล้ว";
            }
        }
        catch (Exception ex) { StatusMessage = $"ไม่สามารถเพิ่มสินค้าได้: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteProductAsync()
    {
        if (_productRepository is null) { StatusMessage = "Product repository not available."; return; }
        if (SelectedRow is null) { StatusMessage = "กรุณาเลือกสินค้าที่ต้องการลบ"; return; }
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var name = SelectedRow.Name;
            await _productRepository.DeleteAsync(SelectedRow.ProductId);
            await LoadRowsAsync();
            StatusMessage = $"ลบสินค้า '{name}' เรียบร้อยแล้ว";
        }
        catch (Exception ex) { StatusMessage = $"ไม่สามารถลบสินค้าได้: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    public void CompleteAddProduct(Product? product) => _addProductTcs?.TrySetResult(product);

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

        RefreshLineNumbers();
    }

    private void RefreshLineNumbers()
    {
        for (int i = 0; i < Rows.Count; i++)
            Rows[i].LineNumber = i + 1;
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

    [ObservableProperty]
    private int _lineNumber;
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
