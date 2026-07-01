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

                vm.OpenPeriodSelectorRequested += async (_, _) =>
                {
                    var selectorVm = new PeriodSelectorViewModel
                    {
                        SelectedYear = vm.SelectedYear,
                        SelectedMonth = vm.SelectedMonth
                    };
                    var dialog = new PeriodSelectorWindow { DataContext = selectorVm };
                    await dialog.ShowDialog(this);
                    if (selectorVm.IsConfirmed)
                        await vm.ApplyPeriodAsync(selectorVm.SelectedYear, selectorVm.SelectedMonth);
                };
            }
        };
    }
}