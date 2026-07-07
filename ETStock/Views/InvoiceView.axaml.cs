using Avalonia.Controls;
using ETStock.ViewModels;

namespace ETStock.Views;

public partial class InvoiceView : UserControl
{
    private InvoiceViewModel? _vm;
    private PdfPreviewWindow? _previewWindow;

    public InvoiceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
            _vm.PdfPreviewRequested -= OnPdfPreviewRequested;

        _vm = DataContext as InvoiceViewModel;

        if (_vm is not null)
            _vm.PdfPreviewRequested += OnPdfPreviewRequested;
    }

    private void OnPdfPreviewRequested(byte[] pdfBytes)
    {
        // Reuse the same window — recreating WebView2 fails with 0x800700AA if called
        // shortly after closing, because the browser process hasn't fully exited yet.
        _previewWindow ??= new PdfPreviewWindow();
        _previewWindow.LoadPdf(pdfBytes);

        if (!_previewWindow.IsVisible)
            _previewWindow.Show();
        else
            _previewWindow.Activate();
    }
}
