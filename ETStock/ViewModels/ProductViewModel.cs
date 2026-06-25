using CommunityToolkit.Mvvm.ComponentModel;
using ETStock.Data.Repositories;

namespace ETStock.ViewModels;

public partial class ProductViewModel : ViewModelBase
{
    internal IProductRepository? ProductRepository => _productRepository;

    [ObservableProperty]
    private string _title = "สินค้า / สต๊อก";
}
