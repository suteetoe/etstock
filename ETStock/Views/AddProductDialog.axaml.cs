using Avalonia.Controls;
using ETStock.Models;
using ETStock.ViewModels;

namespace ETStock.Views;

public partial class AddProductDialog : Window
{
    public AddProductDialog()
    {
        InitializeComponent();
        var vm = new AddProductDialogViewModel();
        vm.DialogCompleted += (_, result) => Close(result);
        DataContext = vm;
    }
}
