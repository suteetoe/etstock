using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Data.Repositories;
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
        var companyRepository = _serviceScope?.ServiceProvider.GetService<ICompanyRepository>();
        var productRepository = _serviceScope?.ServiceProvider.GetService<IProductRepository>();
        var company = companyRepository is not null
            ? new CompanyViewModel(companyRepository)
            : new CompanyViewModel();

        var product = productRepository is not null
            ? new ProductViewModel(productRepository)
            : new ProductViewModel();
        var invoice = new InvoiceViewModel();

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
