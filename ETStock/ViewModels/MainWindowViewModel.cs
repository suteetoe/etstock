using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ETStock.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    public ObservableCollection<NavItem> NavItems { get; } = [];

    public MainWindowViewModel()
    {
        var home = new HomeViewModel();
        var company = new CompanyViewModel();
        var product = new ProductViewModel();
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
