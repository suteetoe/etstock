using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Data.Repositories;
using ETStock.Models;

namespace ETStock.ViewModels;

public partial class CompanyViewModel : ViewModelBase
{
    private readonly ICompanyRepository? _repository;

    public CompanyViewModel() { }

    public CompanyViewModel(ICompanyRepository repository)
    {
        _repository = repository;
    }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _taxId = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _branchName = string.Empty;
    [ObservableProperty] private string _branchCode = "00000";
    [ObservableProperty] private string _phoneNumber = string.Empty;
    [ObservableProperty] private string _invoicePrefix = string.Empty;
    [ObservableProperty] private string _vatRate = "7";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_repository is null) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var company = await _repository.GetAsync();
            if (company is not null)
            {
                Name = company.Name;
                TaxId = company.TaxId;
                Address = company.Address;
                BranchName = company.BranchName;
                BranchCode = company.BranchCode;
                PhoneNumber = company.PhoneNumber;
                InvoicePrefix = company.InvoicePrefix;
                VatRate = (company.VatRate * 100).ToString("0.##");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"โหลดข้อมูลไม่สำเร็จ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_repository is null) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            if (!decimal.TryParse(VatRate, out var vatPct))
                vatPct = 7m;

            var company = new Company
            {
                Name = Name,
                TaxId = TaxId,
                Address = Address,
                BranchName = BranchName,
                BranchCode = BranchCode,
                PhoneNumber = PhoneNumber,
                InvoicePrefix = InvoicePrefix,
                VatRate = vatPct / 100m
            };
            await _repository.UpsertAsync(company);
            StatusMessage = "บันทึกสำเร็จ";
        }
        catch (Exception ex)
        {
            StatusMessage = $"บันทึกไม่สำเร็จ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
