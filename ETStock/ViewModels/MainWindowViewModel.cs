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

    public MainWindowViewModel() : this(DateTime.Today.Year, DateTime.Today.Month) { }

    public MainWindowViewModel(int year, int month)
    {
        _serviceScope = Program.Services?.CreateScope();
        var serviceProvider = _serviceScope?.ServiceProvider;

        var repo = serviceProvider?.GetService<ICompanyRepository>();
        CompanyPage = repo is not null ? new CompanyViewModel(repo) : new CompanyViewModel();

        var stockRepo = serviceProvider?.GetService<IMonthlyStockRepository>();
        var invoiceGenerator = serviceProvider?.GetService<IInvoiceGeneratorService>();
        StockPage = (stockRepo is not null && invoiceGenerator is not null)
            ? new ProductViewModel(stockRepo, invoiceGenerator, year, month)
            : stockRepo is not null
                ? new ProductViewModel(stockRepo, year, month)
                : new ProductViewModel(year, month);

        var invoiceRepo = serviceProvider?.GetService<IAbbrInvoiceRepository>();
        var printService = serviceProvider?.GetService<IInvoicePrintService>();
        InvoicePage = invoiceRepo is not null
            ? new InvoiceViewModel(invoiceRepo, printService)
            : new InvoiceViewModel();
    }

    [RelayCommand]
    private void OpenCompanyDialog() => OpenCompanyDialogRequested?.Invoke(this, EventArgs.Empty);
}
