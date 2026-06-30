using Avalonia.Controls;
using ETStock.ViewModels;

namespace ETStock.Views;

public partial class PeriodSelectorWindow : Window
{
    public PeriodSelectorWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is PeriodSelectorViewModel vm)
            {
                vm.ConfirmRequested += (_, _) => Close();
                vm.CancelRequested += (_, _) => Close();
            }
        };
    }
}
