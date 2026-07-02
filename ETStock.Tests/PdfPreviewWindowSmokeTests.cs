using System.Reflection;
using Avalonia.Controls;
using ETStock.Views;
using Xunit;

namespace ETStock.Tests;

/// <summary>
/// Structural smoke tests for PdfPreviewWindow (phase pdf-preview-webview).
/// Full instantiation requires Avalonia's native display context (NativeWebView),
/// so we use reflection to verify the public contract without spinning up a window.
/// </summary>
public class PdfPreviewWindowSmokeTests
{
    private static readonly Type WindowType = typeof(PdfPreviewWindow);

    [Fact]
    public void PdfPreviewWindow_InheritsFromAvaloniaWindow()
    {
        Assert.True(
            typeof(Window).IsAssignableFrom(WindowType),
            "PdfPreviewWindow must extend Avalonia.Controls.Window");
    }

    [Fact]
    public void PdfPreviewWindow_HasParameterlessConstructor()
    {
        var ctor = WindowType.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(ctor);
    }

    [Fact]
    public void PdfPreviewWindow_HasByteArrayConstructor()
    {
        var ctor = WindowType.GetConstructor([typeof(byte[])]);
        Assert.NotNull(ctor);
    }

    [Fact]
    public void PdfPreviewWindow_DoesNotReferenceMuPDF()
    {
        // Verify no MuPDF type remains in the assembly referencing this view.
        var assembly = WindowType.Assembly;
        var muPdfTypes = assembly.GetReferencedAssemblies()
            .Where(a => a.Name != null && a.Name.Contains("MuPDF", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(muPdfTypes);
    }

    [Fact]
    public void PdfPreviewWindow_AssemblyReferencesAvaloniaWebView()
    {
        var assembly = WindowType.Assembly;
        var hasWebView = assembly.GetReferencedAssemblies()
            .Any(a => a.Name != null && a.Name.Contains("WebView", StringComparison.OrdinalIgnoreCase));

        Assert.True(hasWebView, "ETStock assembly should reference an Avalonia WebView package");
    }

    [Fact(Skip = "NativeWebView requires a native display context — cannot run headless in xUnit")]
    public void PdfPreviewWindow_CanBeInstantiatedWithPdfBytes()
    {
        // %PDF magic header — a minimal valid-looking PDF byte sequence
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
        var window = new PdfPreviewWindow(pdfBytes);
        Assert.NotNull(window);
    }
}
