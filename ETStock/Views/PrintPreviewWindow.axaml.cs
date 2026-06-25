using Avalonia.Controls;
using ETStock.ViewModels;

namespace ETStock.Views;

public partial class PrintPreviewWindow : Window
{
    public PrintPreviewWindow()
    {
        InitializeComponent();
    }

    public PrintPreviewWindow(PrintPreviewViewModel vm) : this()
    {
        DataContext = vm;
        vm.CloseRequested += Close;
    }
}
