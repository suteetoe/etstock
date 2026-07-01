using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ETStock.ViewModels;

namespace ETStock.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OpenCompanyDialogRequested += async (_, _) =>
                {
                    var dialog = new CompanyDialog { DataContext = vm.CompanyPage };
                    await dialog.ShowDialog(this);
                };

                vm.OpenPeriodSelectorRequested += async (_, _) =>
                {
                    var selectorVm = new PeriodSelectorViewModel
                    {
                        SelectedYear = vm.SelectedYear,
                        SelectedMonth = vm.SelectedMonth
                    };
                    var dialog = new PeriodSelectorWindow { DataContext = selectorVm };
                    await dialog.ShowDialog(this);
                    if (selectorVm.IsConfirmed)
                        await vm.ApplyPeriodAsync(selectorVm.SelectedYear, selectorVm.SelectedMonth);
                };

                vm.StockPage.ImportExcelRequested += OnImportExcelRequested;
                vm.StockPage.ImportExcelConfirmRequested += OnImportExcelConfirmRequested;
            }
        };
    }

    private async void OnImportExcelRequested(object? sender, EventArgs e)
    {
        if (sender is not ProductViewModel vm) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "เลือกไฟล์ Excel ยอดยกมา",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Excel files") { Patterns = ["*.xlsx", "*.xls"] }
            ]
        });

        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        vm.CompleteImportExcel(path);
    }

    private async void OnImportExcelConfirmRequested(object? sender, int rowCount)
    {
        if (sender is not ProductViewModel vm)
            return;

        var dialog = new Window
        {
            Title = "ยืนยันการนำเข้าข้อมูล",
            Width = 440,
            Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        bool confirmed = false;
        var okBtn = new Button { Content = "ยืนยันนำเข้า", HorizontalAlignment = HorizontalAlignment.Center };
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
                    Text = $"พบ {rowCount} รายการจากไฟล์ Excel\n" +
                           "ระบบจะล้างรายการในตารางและนำข้อมูลใหม่ใส่แทน\n" +
                           "ต้องการดำเนินการต่อหรือไม่?",
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

        await dialog.ShowDialog(this);
        vm.CompleteImportExcelConfirm(confirmed);
    }
}
