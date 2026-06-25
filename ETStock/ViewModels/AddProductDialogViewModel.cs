using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Models;

namespace ETStock.ViewModels;

public partial class AddProductDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _unit = string.Empty;
    [ObservableProperty] private decimal _costPrice;
    [ObservableProperty] private decimal _sellPrice;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public event EventHandler<Product?>? DialogCompleted;

    [RelayCommand]
    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(Code)) { ErrorMessage = "กรุณาระบุรหัสสินค้า"; return; }
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "กรุณาระบุชื่อสินค้า"; return; }
        ErrorMessage = string.Empty;
        DialogCompleted?.Invoke(this, new Product
        {
            Code = Code.Trim(),
            Name = Name.Trim(),
            Unit = Unit.Trim(),
            CostPrice = CostPrice,
            SellPrice = SellPrice
        });
    }

    [RelayCommand]
    private void Cancel() => DialogCompleted?.Invoke(this, null);
}
