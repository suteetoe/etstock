using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using System.IO;

namespace ETStock.Views;

public partial class PdfPreviewWindow : Window
{
    // Never point WebView2's UserDataFolder at the install directory — under
    // Program Files a standard user can't write there. Avalonia caches its
    // internal environment per UserDataFolder, so giving the same writable
    // per-user folder every time reuses the same browser process/environment
    // across show/hide cycles, avoiding the 0x800700AA "resource in use" error.
    private static readonly string SharedUserDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ETStock", "WebView2");

    private string _tempPdfPath = string.Empty;

    public PdfPreviewWindow()
    {
        InitializeComponent();
        WebView.EnvironmentRequested += OnEnvironmentRequested;
        Opened += OnOpened;

        Closing += (_, e) =>
        {
            // The title-bar close button reports WindowClosing (not Undefined) —
            // intercept it so the window is hidden and reused instead of disposed,
            // which would otherwise make the next Show() throw "Cannot re-show a
            // closed window". Let owner/shutdown-driven closes proceed normally.
            if (e.CloseReason is WindowCloseReason.Undefined or WindowCloseReason.WindowClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    public void LoadPdf(byte[] pdfBytes)
    {
        DeleteTempFile();
        _tempPdfPath = Path.Combine(Path.GetTempPath(), $"etstock_invoice_{Guid.NewGuid():N}.pdf");
        File.WriteAllBytes(_tempPdfPath, pdfBytes);

        //var dialog = new Window
        //{
        //    Title = _tempPdfPath,
        //    Width = 400,
        //    Height = 160,
        //    WindowStartupLocation = WindowStartupLocation.CenterOwner,
        //    CanResize = false
        //};

        //var topLevel = TopLevel.GetTopLevel(this) as Window;
        //dialog.ShowDialog(topLevel);

        if (IsVisible)
            WebView.Source = new Uri(_tempPdfPath);
    }

    private void OnEnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs e)
    {
        if (e is WindowsWebView2EnvironmentRequestedEventArgs webView2)
        {
            Directory.CreateDirectory(SharedUserDataFolder);
            webView2.ProfileName = "AvaloniaUser";
            webView2.UserDataFolder = SharedUserDataFolder;
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_tempPdfPath))
            WebView.Source = new Uri(_tempPdfPath);
    }

    private void DeleteTempFile()
    {
        try { if (File.Exists(_tempPdfPath)) File.Delete(_tempPdfPath); } catch { }
    }

    private void Print_Click(object? sender, RoutedEventArgs e)
    {
        WebView.ShowPrintUI();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Hide();
    }
}
