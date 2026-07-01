using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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
        {
            _vm.AddProductRequested -= OnAddProductRequested;
            _vm.GenerateInvoicesConfirmRequested -= OnGenerateInvoicesConfirmRequested;
            _vm.ScrollToRowRequested -= OnScrollToRowRequested;
        }

        _vm = DataContext as ProductViewModel;

        if (_vm is not null)
        {
            _vm.AddProductRequested += OnAddProductRequested;
            _vm.GenerateInvoicesConfirmRequested += OnGenerateInvoicesConfirmRequested;
            _vm.ScrollToRowRequested += OnScrollToRowRequested;
        }
    }

    private void OnScrollToRowRequested(object? sender, MonthlyStockRowViewModel row)
    {
        ProductListBox.ScrollIntoView(row);
    }

    private async void OnAddProductRequested(object? sender, EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel is null || _vm is null) { _vm?.CompleteAddProduct(null); return; }
        var dialog = new AddProductDialog();
        var result = await dialog.ShowDialog<AddProductResult?>(topLevel);
        _vm.CompleteAddProduct(result);
    }

    private async void OnGenerateInvoicesConfirmRequested(object? sender, int existingCount)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel is null || _vm is null)
        {
            _vm?.CompleteGenerateInvoicesConfirm(false);
            return;
        }

        var dialog = new Window
        {
            Title = "ยืนยันการสร้างใบกำกับ",
            Width = 400,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        bool confirmed = false;
        var okBtn = new Button { Content = "ลบและสร้างใหม่", HorizontalAlignment = HorizontalAlignment.Center };
        var cancelBtn = new Button { Content = "ยกเลิก", HorizontalAlignment = HorizontalAlignment.Center };
        okBtn.Click += (_, _) => { confirmed = true; dialog.Close(); };
        cancelBtn.Click += (_, _) => { dialog.Close(); };

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = $"เดือนนี้มีใบกำกับภาษีอย่างย่ออยู่แล้ว {existingCount} ใบ\nต้องการลบทั้งหมดและสร้างใหม่หรือไม่?",
                    TextWrapping = TextWrapping.Wrap
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children = { okBtn, cancelBtn }
                }
            }
        };

        await dialog.ShowDialog(topLevel);
        _vm.CompleteGenerateInvoicesConfirm(confirmed);
    }

}
