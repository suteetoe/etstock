using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Data.Repositories;
using ETStock.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace ETStock.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceScope? _serviceScope;

    [ObservableProperty]
    private ViewModelBase _currentPage;

    public ObservableCollection<NavItem> NavItems { get; } = [];

    public MainWindowViewModel()
    {
        var home = new HomeViewModel();

        _serviceScope = Program.Services?.CreateScope();
        var serviceProvider = _serviceScope?.ServiceProvider;

        var repo = serviceProvider?.GetService<ICompanyRepository>();
        var company = repo is not null ? new CompanyViewModel(repo) : new CompanyViewModel();

        var stockRepo = serviceProvider?.GetService<IMonthlyStockRepository>();
        var product = stockRepo is not null
            ? new ProductViewModel(stockRepo)
            : new ProductViewModel();
        var invoiceRepo = serviceProvider?.GetService<IAbbrInvoiceRepository>();
        var printService = serviceProvider?.GetService<IInvoicePrintService>();
        var invoice = invoiceRepo is not null
            ? new InvoiceViewModel(invoiceRepo, printService)
            : new InvoiceViewModel();

        NavItems.Add(new NavItem("หน้าแรก", home));
        NavItems.Add(new NavItem("ข้อมูลบริษัท", company));
        NavItems.Add(new NavItem("สินค้า / สต๊อก", product));
        NavItems.Add(new NavItem("ใบกำกับภาษี", invoice));

        _currentPage = home;
    }

    [RelayCommand]
    private void Navigate(NavItem item) => CurrentPage = item.Page;
}

public record NavItem(string Label, ViewModelBase Page);
