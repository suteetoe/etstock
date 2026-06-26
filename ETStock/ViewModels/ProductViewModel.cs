using CommunityToolkit.Mvvm.ComponentModel;

namespace ETStock.ViewModels;

public partial class ProductViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "สินค้า / สต๊อก";
}
