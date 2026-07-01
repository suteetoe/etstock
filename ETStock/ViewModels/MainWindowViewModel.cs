using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Data.Repositories;
using ETStock.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ETStock.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceScope? _serviceScope;

    public CompanyViewModel CompanyPage { get; }
    public ProductViewModel StockPage { get; }
    public InvoiceViewModel InvoicePage { get; }

    public event EventHandler? OpenCompanyDialogRequested;
    public event EventHandler? OpenPeriodSelectorRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeriodLabel))]
    private int _selectedYear;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeriodLabel))]
    private int _selectedMonth;

    public string PeriodLabel => $"ปี {SelectedYear}  เดือน {SelectedMonth:00}";

    partial void OnSelectedYearChanged(int value)
    {
        StockPage.SelectedYear = value;
        InvoicePage.SelectedYear = value;
    }

    partial void OnSelectedMonthChanged(int value)
    {
        StockPage.SelectedMonth = value;
        InvoicePage.SelectedMonth = value;
    }

    [RelayCommand]
    private void OpenPeriodSelector() => OpenPeriodSelectorRequested?.Invoke(this, EventArgs.Empty);

    public MainWindowViewModel() : this(DateTime.Today.Year, DateTime.Today.Month) { }

    public MainWindowViewModel(int year, int month)
    {
        _selectedYear = year;
        _selectedMonth = month;

        _serviceScope = Program.Services?.CreateScope();
        var serviceProvider = _serviceScope?.ServiceProvider;

        var repo = serviceProvider?.GetService<ICompanyRepository>();
        CompanyPage = repo is not null ? new CompanyViewModel(repo) : new CompanyViewModel();

        var stockRepo = serviceProvider?.GetService<IMonthlyStockRepository>();
        var invoiceGenerator = serviceProvider?.GetService<IInvoiceGeneratorService>();
        var excelImporter = serviceProvider?.GetService<IExcelImportService>();
        StockPage = (stockRepo is not null && invoiceGenerator is not null && excelImporter is not null)
            ? new ProductViewModel(stockRepo, invoiceGenerator, excelImporter, year, month)
            : (stockRepo is not null && invoiceGenerator is not null)
                ? new ProductViewModel(stockRepo, invoiceGenerator, year, month)
                : stockRepo is not null
                    ? new ProductViewModel(stockRepo, year, month)
                    : new ProductViewModel(year, month);

        var invoiceRepo = serviceProvider?.GetService<IAbbrInvoiceRepository>();
        var printService = serviceProvider?.GetService<IInvoicePrintService>();
        InvoicePage = invoiceRepo is not null
            ? new InvoiceViewModel(invoiceRepo, printService)
            : new InvoiceViewModel();

        InvoicePage.SelectedYear = year;
        InvoicePage.SelectedMonth = month;

        StockPage.InvoicesGenerated += InvoicePage.RequestLatestRunningRefresh;

        // Sequential initial load to avoid DbContext concurrency errors
        _ = ApplyPeriodAsync(year, month);
    }

    public async Task ApplyPeriodAsync(int year, int month)
    {
        SelectedYear = year;
        SelectedMonth = month;
        await StockPage.LoadCommand.ExecuteAsync(null);
        await InvoicePage.LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void OpenCompanyDialog() => OpenCompanyDialogRequested?.Invoke(this, EventArgs.Empty);
}
