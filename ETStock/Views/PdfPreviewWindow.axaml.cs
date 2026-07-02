using Avalonia.Controls;
using Avalonia.Threading;
using MuPDFCore;
using System.Diagnostics;
using System.IO;

namespace ETStock.Views;

public partial class PdfPreviewWindow : Window
{
    private byte[] _pdfBytes = [];
    private string _tempPdfPath = string.Empty;

    public PdfPreviewWindow()
    {
        InitializeComponent();
    }

    public PdfPreviewWindow(byte[] pdfBytes) : this()
    {
        _pdfBytes = pdfBytes;
        _tempPdfPath = Path.Combine(Path.GetTempPath(), $"etstock_invoice_{Guid.NewGuid():N}.pdf");

        Opened += (_, _) => Dispatcher.UIThread.Post(InitializePreview);
        Closed += (_, _) => Renderer.ReleaseResources();
    }

    private void InitializePreview()
    {
        Renderer.Initialize(
            _pdfBytes,
            InputFileTypes.PDF,
            offset: 0,
            length: -1,
            threadCount: 0,
            pageNumber: 0,
            resolutionMultiplier: 1.5,
            includeAnnotations: true,
            ocrLanguage: null);

        Renderer.Contain();
    }

    private void ZoomIn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Renderer.ZoomStep(1, null);
    }

    private void ZoomOut_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Renderer.ZoomStep(-1, null);
    }

    private void FitPage_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Renderer.Contain();
    }

    private void Print_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        File.WriteAllBytes(_tempPdfPath, _pdfBytes);

        try
        {
            Process.Start(new ProcessStartInfo(_tempPdfPath)
            {
                UseShellExecute = true,
                Verb = "print"
            });
        }
        catch
        {
            Process.Start(new ProcessStartInfo(_tempPdfPath)
            {
                UseShellExecute = true
            });
        }
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
