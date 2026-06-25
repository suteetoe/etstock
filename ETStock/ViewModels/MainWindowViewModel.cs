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

    public MainWindowViewModel()
    {
        _serviceScope = Program.Services?.CreateScope();
        var serviceProvider = _serviceScope?.ServiceProvider;

        var repo = serviceProvider?.GetService<ICompanyRepository>();
        CompanyPage = repo is not null ? new CompanyViewModel(repo) : new CompanyViewModel();

        var stockRepo = serviceProvider?.GetService<IMonthlyStockRepository>();
        var productRepo = serviceProvider?.GetService<IProductRepository>();
        StockPage = (stockRepo is not null && productRepo is not null)
            ? new ProductViewModel(stockRepo, productRepo)
            : stockRepo is not null
                ? new ProductViewModel(stockRepo)
                : new ProductViewModel();

        var invoiceRepo = serviceProvider?.GetService<IAbbrInvoiceRepository>();
        var printService = serviceProvider?.GetService<IInvoicePrintService>();
        InvoicePage = invoiceRepo is not null
            ? new InvoiceViewModel(invoiceRepo, printService)
            : new InvoiceViewModel();
    }

    [RelayCommand]
    private void OpenCompanyDialog() => OpenCompanyDialogRequested?.Invoke(this, EventArgs.Empty);
}
