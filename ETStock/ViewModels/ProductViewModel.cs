using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Data.Repositories;
using ETStock.Models;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ETStock.ViewModels;

public partial class ProductViewModel : ViewModelBase
{
    public const string DuplicateCodeMessage = "รหัสสินค้านี้มีอยู่แล้ว";

    private readonly IProductRepository? _repository;

    public ProductViewModel()
    {
        var today = DateTime.Today;
        _selectedYear = today.Year.ToString(CultureInfo.InvariantCulture);
        _selectedMonth = today.Month;
    }

    public ProductViewModel(IProductRepository repository)
        : this()
    {
        _repository = repository;
        _ = LoadAsync();
    }

    public ObservableCollection<int> Months { get; } =
        new(Enumerable.Range(1, 12));

    public ObservableCollection<ProductStockRowViewModel> Products { get; } = [];

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _unit = string.Empty;
    [ObservableProperty] private string _costPrice = string.Empty;
    [ObservableProperty] private string _sellPrice = string.Empty;
    [ObservableProperty] private string _selectedYear;
    [ObservableProperty] private int _selectedMonth;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    [RelayCommand]
    private async Task AddProductAsync()
    {
        if (_repository is null)
            return;

        StatusMessage = string.Empty;

        var code = Code.Trim();
        var name = Name.Trim();
        var unit = Unit.Trim();

        if (string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(unit))
        {
            StatusMessage = "กรุณากรอกรหัส ชื่อ และหน่วยสินค้า";
            return;
        }

        if (!TryParseNonNegativePrice(CostPrice, out var costPrice)
            || !TryParseNonNegativePrice(SellPrice, out var sellPrice))
        {
            StatusMessage = "ราคาทุนและราคาขายต้องเป็นตัวเลขที่ไม่ติดลบ";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _repository.AddAsync(new Product
            {
                Code = code,
                Name = name,
                Unit = unit,
                CostPrice = costPrice,
                SellPrice = sellPrice
            });

            if (result == ProductWriteResult.DuplicateCode)
            {
                StatusMessage = DuplicateCodeMessage;
                return;
            }

            Code = string.Empty;
            Name = string.Empty;
            Unit = string.Empty;
            CostPrice = string.Empty;
            SellPrice = string.Empty;
            StatusMessage = "เพิ่มสินค้าสำเร็จ";
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"เพิ่มสินค้าไม่สำเร็จ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_repository is null)
            return;

        StatusMessage = string.Empty;
        if (!TryGetSelectedPeriod(out _, out _))
            return;

        IsBusy = true;
        try
        {
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"โหลดสินค้าไม่สำเร็จ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadProductsAsync()
    {
        if (_repository is null || !TryGetSelectedPeriod(out var year, out var month))
            return;

        var products = await _repository.GetAllWithMonthlyStockAsync(year, month);
        Products.Clear();

        foreach (var product in products)
            Products.Add(new ProductStockRowViewModel(product, SaveStockAsync));
    }

    private async Task SaveStockAsync(ProductStockRowViewModel row)
    {
        if (_repository is null || !TryGetSelectedPeriod(out var year, out var month))
            return;

        row.IsBusy = true;
        row.StatusMessage = string.Empty;
        try
        {
            var saved = await _repository.UpsertMonthlyStockAsync(new MonthlyStockInput(
                row.ProductId,
                year,
                month,
                row.OpeningQty,
                row.BuyQty,
                row.SellFullQty,
                row.SellPosQty));

            row.Apply(saved);
            row.StatusMessage = "บันทึกแล้ว";
        }
        catch (Exception ex)
        {
            row.StatusMessage = $"บันทึกไม่สำเร็จ: {ex.Message}";
        }
        finally
        {
            row.IsBusy = false;
        }
    }

    private bool TryGetSelectedPeriod(out int year, out int month)
    {
        month = SelectedMonth;
        if (!int.TryParse(SelectedYear, NumberStyles.None, CultureInfo.InvariantCulture, out year)
            || year <= 0
            || month is < 1 or > 12)
        {
            StatusMessage = "กรุณาระบุปีและเดือนให้ถูกต้อง";
            return false;
        }

        return true;
    }

    private static bool TryParseNonNegativePrice(string text, out decimal value)
    {
        var styles = NumberStyles.Number;
        return (decimal.TryParse(text, styles, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out value))
               && value >= 0;
    }
}

public partial class ProductStockRowViewModel : ViewModelBase
{
    private readonly Func<ProductStockRowViewModel, Task> _save;

    public ProductStockRowViewModel(
        ProductWithStock product,
        Func<ProductStockRowViewModel, Task> save)
    {
        _save = save;
        ProductId = product.Id;
        Code = product.Code;
        Name = product.Name;
        Unit = product.Unit;
        CostPrice = product.CostPrice;

        if (product.MonthlyStock is not null)
            Apply(product.MonthlyStock);
    }

    public int ProductId { get; }
    public string Code { get; }
    public string Name { get; }
    public string Unit { get; }
    public decimal CostPrice { get; }
    public decimal ClosingQty => OpeningQty + BuyQty - SellFullQty - SellPosQty;

    [ObservableProperty] private decimal _openingQty;
    [ObservableProperty] private decimal _buyQty;
    [ObservableProperty] private decimal _sellFullQty;
    [ObservableProperty] private decimal _sellPosQty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    [RelayCommand]
    private Task SaveAsync() => _save(this);

    public void Apply(MonthlyStockSnapshot stock)
    {
        OpeningQty = stock.OpeningQty;
        BuyQty = stock.BuyQty;
        SellFullQty = stock.SellFullQty;
        SellPosQty = stock.SellPosQty;
    }

    partial void OnOpeningQtyChanged(decimal value) => OnPropertyChanged(nameof(ClosingQty));
    partial void OnBuyQtyChanged(decimal value) => OnPropertyChanged(nameof(ClosingQty));
    partial void OnSellFullQtyChanged(decimal value) => OnPropertyChanged(nameof(ClosingQty));
    partial void OnSellPosQtyChanged(decimal value) => OnPropertyChanged(nameof(ClosingQty));
}
