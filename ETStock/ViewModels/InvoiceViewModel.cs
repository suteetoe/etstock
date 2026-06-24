using CommunityToolkit.Mvvm.ComponentModel;

namespace ETStock.ViewModels;

public partial class InvoiceViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "ใบกำกับภาษี";
}
