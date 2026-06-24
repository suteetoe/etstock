using CommunityToolkit.Mvvm.ComponentModel;

namespace ETStock.ViewModels;

public partial class CompanyViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "ข้อมูลบริษัท";
}
