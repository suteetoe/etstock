using Avalonia.Controls;
using ETStock.ViewModels;

namespace ETStock.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OpenCompanyDialogRequested += async (_, _) =>
                {
                    var dialog = new CompanyDialog { DataContext = vm.CompanyPage };
                    await dialog.ShowDialog(this);
                };
            }
        };
    }
}