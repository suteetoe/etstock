using Avalonia.Controls;
using ETStock.ViewModels;

namespace ETStock.Views;

public partial class InvoiceView : UserControl
{
    public InvoiceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is InvoiceViewModel vm)
            vm.PrintPreviewRequested += OnPrintPreviewRequested;
    }

    private async void OnPrintPreviewRequested(PrintPreviewViewModel previewVm)
    {
        var window = new PrintPreviewWindow(previewVm);
        if (TopLevel.GetTopLevel(this) is Window parent)
            await window.ShowDialog(parent);
        else
            window.Show();
    }
}
