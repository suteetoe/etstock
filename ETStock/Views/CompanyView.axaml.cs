using Avalonia.Controls;
using ETStock.ViewModels;

namespace ETStock.Views;

public partial class CompanyView : UserControl
{
    public CompanyView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CompanyViewModel vm)
                await vm.LoadCommand.ExecuteAsync(null);
        };
    }
}
