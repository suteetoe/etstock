using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Data.Repositories;
using ETStock.Models;

namespace ETStock.ViewModels;

public partial class InvoiceViewModel : ViewModelBase
{
    private readonly IAbbrInvoiceRepository? _repository;

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

    public ObservableCollection<AbbrInvoice> Invoices { get; } = [];

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private int _selectedMonth;

    [ObservableProperty]
    private AbbrInvoice? _selectedInvoice;

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
            foreach (var invoice in invoices)
            {
                Invoices.Add(invoice);
            }

            StatusMessage = $"โหลดสำเร็จ {Invoices.Count} ใบกำกับภาษี สำหรับ {SelectedMonth:00}/{SelectedYear}";
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
    private async Task DeleteAsync()
    {
        if (!CanRun()) return;
        if (SelectedInvoice is null) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            await _repository!.DeleteAsync(SelectedInvoice.Id);
            await ReloadAsync();
            StatusMessage = "ลบใบกำกับภาษีเรียบร้อยแล้ว";
        }
        catch (Exception ex)
        {
            StatusMessage = $"ไม่สามารถลบข้อมูลได้: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddInvoiceAsync()
    {
        if (!CanRun()) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var newInvoice = new AbbrInvoice
            {
                InvoiceNo = "NEW",
                InvoiceDate = DateTime.Today,
                TaxYear = SelectedYear,
                TaxMonth = SelectedMonth,
                TotalAmount = 0,
                VatAmount = 0,
                Items = []
            };

            await _repository!.SaveAsync(newInvoice);
            await ReloadAsync();
            StatusMessage = "เพิ่มใบกำกับภาษีใหม่เรียบร้อยแล้ว";
        }
        catch (Exception ex)
        {
            StatusMessage = $"ไม่สามารถเพิ่มข้อมูลได้: {ex.Message}";
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
        foreach (var invoice in invoices)
        {
            Invoices.Add(invoice);
        }
    }
}
