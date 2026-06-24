using CommunityToolkit.Mvvm.ComponentModel;

namespace ETStock.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "หน้าแรก";
}
