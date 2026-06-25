using Avalonia.Controls;
using ETStock.Models;
using ETStock.ViewModels;

namespace ETStock.Views;

public partial class ProductView : UserControl
{
    public ProductView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private ProductViewModel? _vm;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
            _vm.AddProductRequested -= OnAddProductRequested;

        _vm = DataContext as ProductViewModel;

        if (_vm is not null)
            _vm.AddProductRequested += OnAddProductRequested;
    }

    private async void OnAddProductRequested(object? sender, EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel is null || _vm is null) { _vm?.CompleteAddProduct(null); return; }
        var dialog = new AddProductDialog();
        var result = await dialog.ShowDialog<Product?>(topLevel);
        _vm.CompleteAddProduct(result);
    }
}
