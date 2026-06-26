using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ETStock.ViewModels;

public sealed record AddProductResult(
    string ProductName, string Unit, decimal CostPrice, decimal SellPrice, decimal BalanceQty);

public partial class AddProductDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _unit = string.Empty;
    [ObservableProperty] private decimal _balanceQty;
    [ObservableProperty] private decimal _costPrice;
    [ObservableProperty] private decimal _sellPrice;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public event EventHandler<AddProductResult?>? DialogCompleted;

    [RelayCommand]
    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "กรุณาระบุชื่อสินค้า"; return; }
        ErrorMessage = string.Empty;
        DialogCompleted?.Invoke(this, new AddProductResult(Name.Trim(), Unit.Trim(), CostPrice, SellPrice, BalanceQty));
    }

    [RelayCommand]
    private void Cancel() => DialogCompleted?.Invoke(this, null);
}
