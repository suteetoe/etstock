using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Data.Repositories;
using ETStock.Services;

namespace ETStock.ViewModels;

public partial class ProductViewModel
{
    private readonly IMonthlyStockRepository? _repository;
    private readonly IInvoiceGeneratorService? _invoiceGenerator;

    public event EventHandler? AddProductRequested;
    public event EventHandler<int>? GenerateInvoicesConfirmRequested;
    public event Action? InvoicesGenerated;

    private TaskCompletionSource<AddProductResult?>? _addProductTcs;
    private TaskCompletionSource<bool>? _generateConfirmTcs;

    public ProductViewModel()
    {
        var today = DateTime.Today;
        _selectedYear = today.Year;
        _selectedMonth = today.Month;
    }

    public ProductViewModel(int year, int month)
    {
        _selectedYear = year;
        _selectedMonth = month;
    }

    public ProductViewModel(IMonthlyStockRepository repository)
        : this()
    {
        _repository = repository;
    }

    public ProductViewModel(IMonthlyStockRepository repository, int year, int month)
        : this(year, month)
    {
        _repository = repository;
    }

    public ProductViewModel(IMonthlyStockRepository repository, IInvoiceGeneratorService invoiceGenerator, int year, int month)
        : this(year, month)
    {
        _repository = repository;
        _invoiceGenerator = invoiceGenerator;
    }

    public void CompleteGenerateInvoicesConfirm(bool confirmed) =>
        _generateConfirmTcs?.TrySetResult(confirmed);

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
        if (_repository is null) { StatusMessage = "Monthly stock repository is not available."; return; }
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        _addProductTcs = new TaskCompletionSource<AddProductResult?>();
        AddProductRequested?.Invoke(this, EventArgs.Empty);
        var dialogResult = await _addProductTcs.Task;
        _addProductTcs = null;
        try
        {
            if (dialogResult is not null)
            {
                var input = new MonthlyStockInput(
                    dialogResult.ProductName,
                    dialogResult.Unit,
                    dialogResult.CostPrice,
                    dialogResult.SellPrice,
                    SelectedYear, SelectedMonth,
                    dialogResult.BalanceQty, 0, 0, 0);
                await _repository.SaveAsync(input);
                await LoadRowsAsync();
                StatusMessage = $"เพิ่มสินค้า '{dialogResult.ProductName}' เรียบร้อยแล้ว";
            }
        }
        catch (Exception ex) { StatusMessage = $"ไม่สามารถเพิ่มสินค้าได้: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteProductAsync()
    {
        if (_repository is null) { StatusMessage = "Monthly stock repository is not available."; return; }
        if (SelectedRow is null) { StatusMessage = "กรุณาเลือกสินค้าที่ต้องการลบ"; return; }
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var name = SelectedRow.Name;
            await _repository.DeleteAsync(SelectedRow.Name, SelectedYear, SelectedMonth);
            await LoadRowsAsync();
            StatusMessage = $"ลบสินค้า '{name}' เรียบร้อยแล้ว";
        }
        catch (Exception ex) { StatusMessage = $"ไม่สามารถลบสินค้าได้: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    public void CompleteAddProduct(AddProductResult? result) => _addProductTcs?.TrySetResult(result);

    [RelayCommand]
    private async Task GenerateInvoicesAsync()
    {
        if (_invoiceGenerator is null) { StatusMessage = "Invoice generator is not available."; return; }
        if (!CanRun()) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var existingCount = await _invoiceGenerator.GetExistingCountAsync(SelectedYear, SelectedMonth);
            bool replaceExisting = false;

            if (existingCount > 0)
            {
                _generateConfirmTcs = new TaskCompletionSource<bool>();
                GenerateInvoicesConfirmRequested?.Invoke(this, existingCount);
                var confirmed = await _generateConfirmTcs.Task;
                _generateConfirmTcs = null;
                if (!confirmed)
                {
                    StatusMessage = "ยกเลิกการสร้างใบกำกับ";
                    return;
                }
                replaceExisting = true;
            }

            var stockLines = Rows
                .Where(r => r.SellPosQty > 0)
                .Select(r => new PosStockLine(r.Name, r.SellPosQty, r.SellPrice))
                .ToList();

            var result = await _invoiceGenerator.GenerateAsync(
                SelectedYear, SelectedMonth, stockLines, replaceExisting, seedStart: null);

            StatusMessage = result is null
                ? "ไม่มียอดขายหน้าร้านในเดือนนี้ (SellPosQty ทุกรายการเป็น 0)"
                : $"สร้างใบกำกับภาษีอย่างย่อ {result.InvoiceCount} ใบ สำเร็จ " +
                  $"(มูลค่ารวม {result.TotalAmount:N2} บาท VAT {result.VatAmount:N2} บาท)";

            if (result is not null)
                InvoicesGenerated?.Invoke();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("ไม่พบเลขที่ใบกำกับล่าสุด"))
        {
            StatusMessage = "ยังไม่มีเลขที่ใบกำกับล่าสุด — กรุณาไปที่แท็บ 'ABB List' แล้วระบุเล่มที่/เลขที่เริ่มต้น";
        }
        catch (Exception ex)
        {
            StatusMessage = $"ไม่สามารถสร้างใบกำกับได้: {ex.Message}";
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
        Name = product.ProductName;
        Unit = product.Unit;
        _costPrice = product.CostPrice;
        _sellPrice = product.SellPrice;

        var stock = product.MonthlyStock;
        StockId = stock?.Id ?? 0;
        _openingQty = stock?.OpeningQty ?? 0;
        _buyQty = stock?.BuyQty ?? 0;
        _sellFullQty = stock?.SellFullQty ?? 0;
        _sellPosQty = stock?.SellPosQty ?? 0;
    }

    public int StockId { get; }
    public string Name { get; }
    public string Unit { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingValue))]
    private decimal _costPrice;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SalesAmount))]
    private decimal _sellPrice;

    public decimal ClosingValue => ClosingQty * CostPrice;

    [ObservableProperty]
    private int _lineNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingQty))]
    [NotifyPropertyChangedFor(nameof(ClosingValue))]
    private decimal _openingQty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingQty))]
    [NotifyPropertyChangedFor(nameof(ClosingValue))]
    private decimal _buyQty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingQty))]
    [NotifyPropertyChangedFor(nameof(ClosingValue))]
    [NotifyPropertyChangedFor(nameof(SalesQty))]
    [NotifyPropertyChangedFor(nameof(SalesAmount))]
    private decimal _sellFullQty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingQty))]
    [NotifyPropertyChangedFor(nameof(ClosingValue))]
    [NotifyPropertyChangedFor(nameof(SalesQty))]
    [NotifyPropertyChangedFor(nameof(SalesAmount))]
    private decimal _sellPosQty;

    public decimal ClosingQty => OpeningQty + BuyQty - SellFullQty - SellPosQty;
    public decimal SalesQty => SellFullQty + SellPosQty;
    public decimal SalesAmount => SalesQty * SellPrice;

    public MonthlyStockInput ToInput(int year, int month) => new(
        Name,
        Unit,
        CostPrice,
        SellPrice,
        year,
        month,
        OpeningQty,
        BuyQty,
        SellFullQty,
        SellPosQty);
}
