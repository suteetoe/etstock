using Avalonia.Controls;
using ETStock.ViewModels;

namespace ETStock.Views;

public partial class InvoiceView : UserControl
{
    private InvoiceViewModel? _vm;

    public InvoiceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
            _vm.PrintPreviewRequested -= OnPrintPreviewRequested;

        _vm = DataContext as InvoiceViewModel;

        if (_vm is not null)
            _vm.PrintPreviewRequested += OnPrintPreviewRequested;
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
