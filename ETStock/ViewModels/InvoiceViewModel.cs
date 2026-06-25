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
        _selectedYear = today.Year + 543;
        _selectedMonth = today.Month;
        _editInvoiceDate = today;
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

    public ObservableCollection<InvoiceItemRowViewModel> EditItems { get; } = [];

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private int _selectedMonth;

    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private string _editInvoiceNo = string.Empty;

    [ObservableProperty]
    private DateTime _editInvoiceDate;

    [ObservableProperty]
    private int _editId;

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

    [RelayCommand]
    private void New()
    {
        EditId = 0;
        EditInvoiceNo = string.Empty;
        EditInvoiceDate = DateTime.Today;
        EditItems.Clear();
        IsFormOpen = true;
    }

    [RelayCommand]
    private async Task EditAsync(InvoiceRowViewModel row)
    {
        if (_repository is null) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var inv = await _repository.GetByIdAsync(row.Id);
            if (inv is null)
            {
                StatusMessage = $"ไม่พบใบกำกับ id={row.Id}";
                return;
            }

            EditId = inv.Id;
            EditInvoiceNo = inv.InvoiceNo;
            EditInvoiceDate = inv.InvoiceDate;

            EditItems.Clear();
            foreach (var item in inv.Items)
            {
                EditItems.Add(new InvoiceItemRowViewModel
                {
                    ProductId = item.ProductId,
                    Qty = item.Qty,
                    Amount = item.Amount,
                    VatAmount = item.VatAmount,
                });
            }

            IsFormOpen = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"ไม่สามารถโหลดใบกำกับได้: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(InvoiceRowViewModel row)
    {
        if (!CanRun()) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            await _repository!.DeleteAsync(row.Id);
            await ReloadAsync();
            StatusMessage = $"ลบใบกำกับ {row.InvoiceNo} แล้ว";
        }
        catch (Exception ex)
        {
            StatusMessage = $"ไม่สามารถลบใบกำกับได้: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        IsFormOpen = false;
        EditItems.Clear();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanRun()) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var items = EditItems.Select(i => new AbbrInvoiceItem
            {
                ProductId = i.ProductId,
                Qty = i.Qty,
                Amount = i.Amount,
                VatAmount = i.VatAmount,
            }).ToList();

            var totalAmount = items.Sum(i => i.Amount + i.VatAmount);
            var vatAmount = items.Sum(i => i.VatAmount);

            var invoice = new AbbrInvoice
            {
                Id = EditId,
                InvoiceNo = EditInvoiceNo,
                InvoiceDate = EditInvoiceDate,
                TaxYear = SelectedYear,
                TaxMonth = SelectedMonth,
                TotalAmount = totalAmount,
                VatAmount = vatAmount,
                Items = items,
            };

            await _repository!.SaveAsync(invoice);
            await ReloadAsync();

            IsFormOpen = false;
            EditItems.Clear();
            StatusMessage = EditId == 0
                ? $"เพิ่มใบกำกับ {EditInvoiceNo} แล้ว"
                : $"บันทึกใบกำกับ {EditInvoiceNo} แล้ว";
        }
        catch (Exception ex)
        {
            StatusMessage = $"ไม่สามารถบันทึกใบกำกับได้: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddItem()
    {
        EditItems.Add(new InvoiceItemRowViewModel());
    }

    [RelayCommand]
    private void RemoveItem(InvoiceItemRowViewModel item)
    {
        EditItems.Remove(item);
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

    private async Task ReloadAsync()
    {
        var invoices = await _repository!.GetByPeriodAsync(SelectedYear, SelectedMonth);
        Invoices.Clear();
        foreach (var inv in invoices)
            Invoices.Add(new InvoiceRowViewModel(inv));

        var summary = await _repository.GetPeriodSummaryAsync(SelectedYear, SelectedMonth);
        SummaryCount = summary.Count;
        SummaryTotal = summary.TotalAmount;
        SummaryVat = summary.VatAmount;
    }
}

public class InvoiceRowViewModel
{
    public int Id { get; }
    public string InvoiceNo { get; }
    public DateTime InvoiceDate { get; }
    public int TaxYear { get; }
    public int TaxMonth { get; }
    public decimal TotalAmount { get; }
    public decimal VatAmount { get; }
    public int ItemCount { get; }

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
    }
}

public partial class InvoiceItemRowViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _productId;

    [ObservableProperty]
    private string _productCode = string.Empty;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private decimal _qty;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private decimal _vatAmount;
}
