using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Data.Repositories;
using ETStock.Models;
using ETStock.Services;

namespace ETStock.ViewModels;

public partial class InvoiceViewModel : ViewModelBase
{
    private readonly IAbbrInvoiceRepository? _repository;
    private readonly IInvoicePrintService? _printService;

    public InvoiceViewModel()
    {
        var today = DateTime.Today;
        _selectedYear = today.Year;
        _selectedMonth = today.Month;
    }

    public InvoiceViewModel(IAbbrInvoiceRepository repository)
        : this()
    {
        _repository = repository;
    }

    public InvoiceViewModel(IAbbrInvoiceRepository repository, IInvoicePrintService? printService)
        : this()
    {
        _repository = repository;
        _printService = printService;
    }

    public ObservableCollection<InvoiceRowViewModel> Invoices { get; } = [];

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private int _selectedMonth;

    [ObservableProperty]
    private int _summaryCount;

    [ObservableProperty]
    private decimal _summaryTotal;

    [ObservableProperty]
    private decimal _summaryVat;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _currentBookNo;

    [ObservableProperty]
    private int _currentDocNo;

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (!CanRun()) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var invoices = await _repository!.GetByPeriodAsync(SelectedYear, SelectedMonth);
            Invoices.Clear();
            foreach (var inv in invoices)
                Invoices.Add(new InvoiceRowViewModel(inv));

            var summary = await _repository.GetPeriodSummaryAsync(SelectedYear, SelectedMonth);
            SummaryCount = summary.Count;
            SummaryTotal = summary.TotalAmount;
            SummaryVat = summary.VatAmount;

            StatusMessage = $"โหลดแล้ว {Invoices.Count} ใบ";

            await LoadLatestRunningAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"ไม่สามารถโหลดข้อมูลได้: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void RequestLatestRunningRefresh() => _ = LoadLatestRunningAsync();

    internal async Task LoadLatestRunningAsync()
    {
        if (_repository is null) return;

        try
        {
            var latest = await _repository.GetLatestRunningAsync();
            if (latest is null)
            {
                CurrentBookNo = 0;
                CurrentDocNo = 0;
                StatusMessage = "ยังไม่มีเลขที่ใบกำกับล่าสุดในระบบ กรุณาระบุเล่มที่และเลขที่เริ่มต้น";
            }
            else
            {
                CurrentBookNo = latest.Value.BookNo;
                CurrentDocNo = latest.Value.RunningNo;
            }
        }
        catch
        {
            // Display-only indicator; failures here should not block the main load flow.
        }
    }

    [RelayCommand]
    private void ToggleExpand(InvoiceRowViewModel row)
    {
        row.IsExpanded = !row.IsExpanded;
    }

    public event Action<PrintPreviewViewModel>? PrintPreviewRequested;

    [RelayCommand]
    private async Task PrintAsync(InvoiceRowViewModel row)
    {
        if (_printService is null)
        {
            StatusMessage = "ไม่สามารถพิมพ์ได้: บริการพิมพ์ไม่พร้อม";
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var doc = await _printService.BuildAsync(row.Id);
            var vm = new PrintPreviewViewModel(doc);
            PrintPreviewRequested?.Invoke(vm);
        }
        catch (Exception ex)
        {
            StatusMessage = $"ไม่สามารถสร้างพรีวิวได้: {ex.Message}";
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
            StatusMessage = "Invoice repository is not available.";
            return false;
        }

        if (SelectedYear < 1 || SelectedMonth is < 1 or > 12)
        {
            StatusMessage = "กรุณาระบุปีและเดือนที่ถูกต้อง (เดือน 1-12)";
            return false;
        }

        return !IsBusy;
    }
}

public partial class InvoiceRowViewModel : ViewModelBase
{
    public int Id { get; }
    public string InvoiceNo { get; }
    public DateTime InvoiceDate { get; }
    public int TaxYear { get; }
    public int TaxMonth { get; }
    public decimal TotalAmount { get; }
    public decimal VatAmount { get; }
    public int ItemCount { get; }
    public IReadOnlyList<InvoiceItemDetailViewModel> Items { get; }

    [ObservableProperty]
    private bool _isExpanded;

    public InvoiceRowViewModel(AbbrInvoice src)
    {
        Id = src.Id;
        InvoiceNo = src.InvoiceNo;
        InvoiceDate = src.InvoiceDate;
        TaxYear = src.TaxYear;
        TaxMonth = src.TaxMonth;
        TotalAmount = src.TotalAmount;
        VatAmount = src.VatAmount;
        ItemCount = src.Items.Count;
        Items = src.Items
            .Select(i => new InvoiceItemDetailViewModel(i.ProductName, i.Qty, i.Amount, i.VatAmount))
            .ToList();
    }
}

public record InvoiceItemDetailViewModel(string ProductName, decimal Qty, decimal Amount, decimal VatAmount);
