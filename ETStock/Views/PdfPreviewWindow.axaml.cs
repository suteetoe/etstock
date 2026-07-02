using Avalonia.Controls;
using Avalonia.Interactivity;
using System.IO;

namespace ETStock.Views;

public partial class PdfPreviewWindow : Window
{
    private string _tempPdfPath = string.Empty;

    public PdfPreviewWindow()
    {
        InitializeComponent();
    }

    public PdfPreviewWindow(byte[] pdfBytes) : this()
    {
        _tempPdfPath = Path.Combine(Path.GetTempPath(), $"etstock_invoice_{Guid.NewGuid():N}.pdf");
        File.WriteAllBytes(_tempPdfPath, pdfBytes);

        Opened += OnOpened;
        Closed += OnClosed;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        WebView.Source = new Uri(_tempPdfPath);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        try
        {
            if (File.Exists(_tempPdfPath))
            {
                File.Delete(_tempPdfPath);
            }
        }
        catch
        {
            // Ignore best-effort temp cleanup failures.
        }
    }

    private void Print_Click(object? sender, RoutedEventArgs e)
    {
        WebView.ShowPrintUI();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
