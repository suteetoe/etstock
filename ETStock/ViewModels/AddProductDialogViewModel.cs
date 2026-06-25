using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ETStock.Models;

namespace ETStock.ViewModels;

public partial class AddProductDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private decimal _costPrice;
    [ObservableProperty] private decimal _sellPrice;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public event EventHandler<Product?>? DialogCompleted;

    [RelayCommand]
    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "กรุณาระบุชื่อสินค้า"; return; }
        ErrorMessage = string.Empty;
        DialogCompleted?.Invoke(this, new Product
        {
            Code = Guid.NewGuid().ToString("N")[..8].ToUpper(),
            Name = Name.Trim(),
            Unit = string.Empty,
            CostPrice = CostPrice,
            SellPrice = SellPrice
        });
    }

    [RelayCommand]
    private void Cancel() => DialogCompleted?.Invoke(this, null);
}
