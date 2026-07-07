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
    private readonly IExcelImportService? _excelImporter;
    private readonly IExcelExportService? _excelExporter;

    public event EventHandler? AddProductRequested;
    public event EventHandler<int>? GenerateInvoicesConfirmRequested;
    public event Action? InvoicesGenerated;
    public event EventHandler? ImportExcelRequested;
    public event EventHandler<int>? ImportExcelConfirmRequested;
    public event EventHandler? ExportExcelRequested;
    public event EventHandler<MonthlyStockRowViewModel>? ScrollToRowRequested;

    private TaskCompletionSource<AddProductResult?>? _addProductTcs;
    private TaskCompletionSource<bool>? _generateConfirmTcs;
    private TaskCompletionSource<string?>? _importExcelTcs;
    private TaskCompletionSource<bool>? _importExcelConfirmTcs;
    private TaskCompletionSource<string?>? _exportExcelTcs;
    private IReadOnlyList<ExcelImportRecord>? _pendingImportRecords;
    private int? _seed;
    private bool _hasUnsavedChanges;

    public void SetSeed(int runningNo) => _seed = runningNo;

    public decimal TotalOpeningQty => Rows.Sum(r => r.OpeningQty);
    public decimal TotalBuyQty => Rows.Sum(r => r.BuyQty);
    public decimal TotalSellPosQty => Rows.Sum(r => r.SellPosQty);
    public decimal TotalSellFullQty => Rows.Sum(r => r.SellFullQty);
    public decimal TotalClosingQty => Rows.Sum(r => r.ClosingQty);
    public decimal TotalCostValue => Rows.Sum(r => r.CostPrice);
    public decimal TotalSellValue => Rows.Sum(r => r.SellPrice);
    public decimal TotalSalesAmount => Rows.Sum(r => r.SalesAmount);
    public decimal TotalClosingValue => Rows.Sum(r => r.ClosingValue);

    private void RecalcTotals()
    {
        OnPropertyChanged(nameof(TotalOpeningQty));
        OnPropertyChanged(nameof(TotalBuyQty));
        OnPropertyChanged(nameof(TotalSellPosQty));
        OnPropertyChanged(nameof(TotalSellFullQty));
        OnPropertyChanged(nameof(TotalClosingQty));
        OnPropertyChanged(nameof(TotalCostValue));
        OnPropertyChanged(nameof(TotalSellValue));
        OnPropertyChanged(nameof(TotalSalesAmount));
        OnPropertyChanged(nameof(TotalClosingValue));
    }

    public ProductViewModel()
    {
        var today = DateTime.Today;
        _selectedYear = today.Year;
        _selectedMonth = today.Month;
        Rows.CollectionChanged += (_, _) => RecalcTotals();
    }

    public ProductViewModel(int year, int month)
    {
        _selectedYear = year;
        _selectedMonth = month;
        Rows.CollectionChanged += (_, _) => RecalcTotals();
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

    public ProductViewModel(IMonthlyStockRepository repository, IInvoiceGeneratorService invoiceGenerator, IExcelImportService excelImporter, int year, int month)
        : this(year, month)
    {
        _repository = repository;
        _invoiceGenerator = invoiceGenerator;
        _excelImporter = excelImporter;
    }

    public ProductViewModel(
        IMonthlyStockRepository repository,
        IInvoiceGeneratorService invoiceGenerator,
        IExcelImportService excelImporter,
        IExcelExportService excelExporter,
        int year,
        int month)
        : this(repository, invoiceGenerator, excelImporter, year, month)
    {
        _excelExporter = excelExporter;
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

    [ObservableProperty]
    private string _searchText = string.Empty;

    [RelayCommand]
    private void SearchProduct()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        var found = Rows.FirstOrDefault(r => r.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        if (found is null)
        {
            StatusMessage = $"ไม่พบสินค้า '{SearchText}'";
            return;
        }
        SelectedRow = found;
        ScrollToRowRequested?.Invoke(this, found);
        StatusMessage = string.Empty;
    }

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

    public void CompleteImportExcel(string? path) => _importExcelTcs?.TrySetResult(path);
    public void CompleteImportExcelConfirm(bool confirmed) => _importExcelConfirmTcs?.TrySetResult(confirmed);
    public void CompleteExportExcel(string? path) => _exportExcelTcs?.TrySetResult(path);

    [RelayCommand]
    private async Task ImportFromExcelAsync()
    {
        if (_excelImporter is null) { StatusMessage = "Excel import service is not available."; return; }
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            _importExcelTcs = new TaskCompletionSource<string?>();
            ImportExcelRequested?.Invoke(this, EventArgs.Empty);
            var filePath = await _importExcelTcs.Task;
            _importExcelTcs = null;

            if (string.IsNullOrEmpty(filePath))
            {
                StatusMessage = string.Empty;
                return;
            }

            _pendingImportRecords = _excelImporter.ReadStockMaster(filePath);

            _importExcelConfirmTcs = new TaskCompletionSource<bool>();
            ImportExcelConfirmRequested?.Invoke(this, _pendingImportRecords.Count);
            var confirmed = await _importExcelConfirmTcs.Task;
            _importExcelConfirmTcs = null;

            if (!confirmed)
            {
                StatusMessage = "ยกเลิกการนำเข้าข้อมูล";
                return;
            }

            Rows.Clear();
            foreach (var record in _pendingImportRecords)
                Rows.Add(new MonthlyStockRowViewModel(record));
            RefreshLineNumbers();
            _hasUnsavedChanges = true;

            StatusMessage = $"นำเข้าข้อมูลสำเร็จ {_pendingImportRecords.Count} รายการ — กด Save เพื่อบันทึก";
        }
        catch (Exception ex)
        {
            StatusMessage = $"ไม่สามารถนำเข้าข้อมูลได้: {ex.Message}";
        }
        finally
        {
            _pendingImportRecords = null;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (_excelExporter is null) { StatusMessage = "Excel export service is not available."; return; }
        if (!CanRun()) return;
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            if (_hasUnsavedChanges)
            {
                StatusMessage = "กำลังบันทึกข้อมูลสต๊อก...";
                foreach (var row in Rows)
                    await _repository!.SaveAsync(row.ToInput(SelectedYear, SelectedMonth));
                await LoadRowsAsync();
                StatusMessage = string.Empty;
            }

            _exportExcelTcs = new TaskCompletionSource<string?>();
            ExportExcelRequested?.Invoke(this, EventArgs.Empty);
            var filePath = await _exportExcelTcs.Task;
            _exportExcelTcs = null;

            if (string.IsNullOrEmpty(filePath))
            {
                StatusMessage = "ยกเลิกการส่งออก";
                return;
            }

            _excelExporter.WriteStock(Rows, SelectedYear, SelectedMonth, filePath);

            StatusMessage = "ส่งออกข้อมูลสำเร็จ";
        }
        catch (Exception ex)
        {
            StatusMessage = $"ไม่สามารถส่งออกข้อมูลได้: {ex.Message}";
        }
        finally
        {
            _exportExcelTcs = null;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GenerateInvoicesAsync()
    {
        if (_invoiceGenerator is null) { StatusMessage = "Invoice generator is not available."; return; }
        if (!CanRun()) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            // Step 1: check for existing invoices this period — confirm with user before replacing
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

            // Step 2: auto-save if there are unsaved changes
            if (_hasUnsavedChanges)
            {
                StatusMessage = "กำลังบันทึกข้อมูลสต๊อก...";
                foreach (var row in Rows)
                    await _repository!.SaveAsync(row.ToInput(SelectedYear, SelectedMonth));
                await LoadRowsAsync();
            }

            // Step 3: generate invoices
            var stockLines = Rows
                .Where(r => r.SellPosQty > 0)
                .Select(r => new PosStockLine(r.Name, r.SellPosQty, r.SellPrice))
                .ToList();

            var result = await _invoiceGenerator.GenerateAsync(
                SelectedYear, SelectedMonth, stockLines, replaceExisting, seedRunningNo: _seed);

            // _seed = null;

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

        foreach (var row in Rows)
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(MonthlyStockRowViewModel.BuyQty)
                    or nameof(MonthlyStockRowViewModel.SellPosQty)
                    or nameof(MonthlyStockRowViewModel.SellFullQty)
                    or nameof(MonthlyStockRowViewModel.CostPrice)
                    or nameof(MonthlyStockRowViewModel.SellPrice))
                {
                    _hasUnsavedChanges = true;
                    RecalcTotals();
                }
            };
        _hasUnsavedChanges = false;
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
        CarryForwardQty = stock?.CarryForwardQty ?? 0;
    }

    public MonthlyStockRowViewModel(ExcelImportRecord record)
    {
        Name = record.ProductName;
        Unit = string.Empty;
        StockId = 0;
        _costPrice = record.CostPrice;
        _sellPrice = record.SellPrice;
        _openingQty = record.OpeningQty;
        _buyQty = 0;
        _sellFullQty = 0;
        _sellPosQty = 0;
    }

    public int StockId { get; }
    public string Name { get; }
    public string Unit { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingValue))]
    [NotifyPropertyChangedFor(nameof(TotalCost))]
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
    [NotifyPropertyChangedFor(nameof(TotalCost))]
    private decimal _sellFullQty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClosingQty))]
    [NotifyPropertyChangedFor(nameof(ClosingValue))]
    [NotifyPropertyChangedFor(nameof(SalesQty))]
    [NotifyPropertyChangedFor(nameof(SalesAmount))]
    [NotifyPropertyChangedFor(nameof(TotalCost))]
    private decimal _sellPosQty;

    public decimal ClosingQty => OpeningQty + BuyQty - SellFullQty - SellPosQty;
    public decimal SalesQty => SellFullQty + SellPosQty;
    public decimal SalesAmount => SellPosQty * SellPrice;
    public decimal TotalCost => SalesQty * CostPrice;
    public decimal CarryForwardQty { get; private set; }

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
