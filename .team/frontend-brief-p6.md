# Frontend Brief — P6: ปุ่มสร้างใบกำกับภาษีอย่างย่อ

## Context
เพิ่มปุ่ม "สร้างใบกำกับภาษีอย่างย่อ" ใน ProductView (Monthly Stock screen)
เมื่อคลิก → นำ SellPosQty ของทุกสินค้าในเดือนนั้นส่งให้ `IInvoiceGeneratorService.GenerateAsync`
ถ้าเดือนนั้นมีใบกำกับอยู่แล้ว → แสดง confirm dialog ก่อน

## Read First
- `ETStock/ViewModels/MonthlyStockViewModels.cs` — ProductViewModel + MonthlyStockRowViewModel
- `ETStock/Views/ProductView.axaml` — AXAML ปัจจุบัน
- `ETStock/Views/ProductView.axaml.cs` — code-behind pattern
- `ETStock/ViewModels/MainWindowViewModel.cs` — DI wiring

## Backend Contract (interface ที่ Backend เตรียมไว้)

```csharp
// ETStock/Services/IInvoiceGeneratorService.cs
namespace ETStock.Services;

public record PosStockLine(string ProductName, decimal SellPosQty, decimal SellPrice);
public record GenerateInvoicesResult(int InvoiceCount, decimal TotalAmount, decimal VatAmount);

public interface IInvoiceGeneratorService
{
    Task<int> GetExistingCountAsync(int taxYear, int taxMonth, CancellationToken ct = default);

    Task<GenerateInvoicesResult?> GenerateAsync(
        int taxYear,
        int taxMonth,
        IReadOnlyList<PosStockLine> stockLines,
        bool replaceExisting = false,
        CancellationToken ct = default);
}
```

ใน namespace `ETStock.Services` (ไฟล์ `ETStock/Services/IInvoiceGeneratorService.cs`)

## Scope (แก้ได้เฉพาะไฟล์เหล่านี้)
- `ETStock/ViewModels/MonthlyStockViewModels.cs`
- `ETStock/Views/ProductView.axaml`
- `ETStock/Views/ProductView.axaml.cs`
- `ETStock/ViewModels/MainWindowViewModel.cs`

## Task

### 1. เพิ่ม interface + records stub ใน services (ถ้า Backend ยังไม่ได้สร้าง)

สร้าง `ETStock/Services/IInvoiceGeneratorService.cs` ตาม Backend Contract ด้านบน
(Backend agent จะ implement ทับในภายหลัง — ไฟล์นี้จำเป็นเพื่อให้ build ผ่าน)

### 2. แก้ `ProductViewModel` ใน `MonthlyStockViewModels.cs`

เพิ่ม field:
```csharp
private readonly IInvoiceGeneratorService? _invoiceGenerator;
```

เพิ่ม constructor overload ที่รับ service ทั้งคู่:
```csharp
public ProductViewModel(IMonthlyStockRepository repository, IInvoiceGeneratorService invoiceGenerator, int year, int month)
    : this(year, month)
{
    _repository = repository;
    _invoiceGenerator = invoiceGenerator;
    _ = LoadAsync();
}
```

เพิ่ม event + TCS pattern (เหมือน AddProductRequested):
```csharp
public event EventHandler<int>? GenerateInvoicesConfirmRequested;  // arg = existing count
private TaskCompletionSource<bool>? _generateConfirmTcs;

public void CompleteGenerateInvoicesConfirm(bool confirmed) =>
    _generateConfirmTcs?.TrySetResult(confirmed);
```

เพิ่ม `GenerateInvoicesCommand`:
```csharp
[RelayCommand]
private async Task GenerateInvoicesAsync()
{
    if (_invoiceGenerator is null) { StatusMessage = "Invoice generator is not available."; return; }
    if (!CanRun()) return;

    IsBusy = true;
    StatusMessage = string.Empty;

    try
    {
        // ตรวจสอบใบที่มีอยู่
        var existingCount = await _invoiceGenerator.GetExistingCountAsync(SelectedYear, SelectedMonth);
        bool replaceExisting = false;

        if (existingCount > 0)
        {
            _generateConfirmTcs = new TaskCompletionSource<bool>();
            GenerateInvoicesConfirmRequested?.Invoke(this, existingCount);
            var confirmed = await _generateConfirmTcs.Task;
            _generateConfirmTcs = null;
            if (!confirmed)
            {
                StatusMessage = "ยกเลิกการสร้างใบกำกับ";
                return;
            }
            replaceExisting = true;
        }

        var stockLines = Rows
            .Where(r => r.SellPosQty > 0)
            .Select(r => new PosStockLine(r.Name, r.SellPosQty, r.SellPrice))
            .ToList();

        var result = await _invoiceGenerator.GenerateAsync(
            SelectedYear, SelectedMonth, stockLines, replaceExisting);

        StatusMessage = result is null
            ? "ไม่มียอดขายหน้าร้านในเดือนนี้ (SellPosQty ทุกรายการเป็น 0)"
            : $"สร้างใบกำกับภาษีอย่างย่อ {result.InvoiceCount} ใบ สำเร็จ " +
              $"(มูลค่ารวม {result.TotalAmount:N2} บาท VAT {result.VatAmount:N2} บาท)";
    }
    catch (Exception ex)
    {
        StatusMessage = $"ไม่สามารถสร้างใบกำกับได้: {ex.Message}";
    }
    finally
    {
        IsBusy = false;
    }
}
```

ต้อง `using ETStock.Services;` ใน MonthlyStockViewModels.cs

### 3. แก้ `MainWindowViewModel.cs`

เพิ่มการ resolve `IInvoiceGeneratorService`:
```csharp
var invoiceGenerator = serviceProvider?.GetService<IInvoiceGeneratorService>();
StockPage = (stockRepo is not null && invoiceGenerator is not null)
    ? new ProductViewModel(stockRepo, invoiceGenerator, year, month)
    : stockRepo is not null
        ? new ProductViewModel(stockRepo, year, month)
        : new ProductViewModel(year, month);
```

เพิ่ม `using ETStock.Services;` ถ้ายังไม่มี

### 4. แก้ `ProductView.axaml`

เพิ่มปุ่มใน **Row 1** (toolbar) โดยเปลี่ยน `ColumnDefinitions` เพิ่ม 1 ช่อง:

ก่อน: `ColumnDefinitions="Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,*"`
หลัง: `ColumnDefinitions="Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,*"`

เพิ่มปุ่มที่ Column 8:
```xml
<Button Grid.Column="8"
        Content="สร้างใบกำกับภาษีอย่างย่อ"
        Command="{Binding GenerateInvoicesCommand}"
        IsEnabled="{Binding !IsBusy}"/>
```

### 5. แก้ `ProductView.axaml.cs`

ผูก event ใน `OnDataContextChanged`:
```csharp
_vm.GenerateInvoicesConfirmRequested += OnGenerateInvoicesConfirmRequested;
// และ unsubscribe ใน if (_vm is not null) { ... }
_vm.GenerateInvoicesConfirmRequested -= OnGenerateInvoicesConfirmRequested;
```

Handler (ใช้ Avalonia built-in dialog หรือ simple window):
```csharp
private async void OnGenerateInvoicesConfirmRequested(object? sender, int existingCount)
{
    var topLevel = TopLevel.GetTopLevel(this) as Window;
    if (topLevel is null || _vm is null)
    {
        _vm?.CompleteGenerateInvoicesConfirm(false);
        return;
    }

    // แสดง confirmation dialog
    var dialog = new Window
    {
        Title = "ยืนยันการสร้างใบกำกับ",
        Width = 400,
        Height = 160,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        CanResize = false
    };

    bool confirmed = false;
    var okBtn = new Button { Content = "ลบและสร้างใหม่", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
    var cancelBtn = new Button { Content = "ยกเลิก", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
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
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            },
            new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 12,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Children = { okBtn, cancelBtn }
            }
        }
    };

    await dialog.ShowDialog(topLevel);
    _vm.CompleteGenerateInvoicesConfirm(confirmed);
}
```

## Acceptance Criteria
- [ ] `dotnet build` ผ่าน 0 errors
- [ ] ปุ่ม "สร้างใบกำกับภาษีอย่างย่อ" ปรากฏใน toolbar
- [ ] ปุ่ม disabled ระหว่าง IsBusy
- [ ] ถ้าไม่มี POS qty → StatusMessage บอกว่าไม่มียอด
- [ ] ถ้ามีใบอยู่แล้ว → แสดง dialog confirm
- [ ] กด "ยกเลิก" → ไม่สร้าง + StatusMessage = "ยกเลิกการสร้างใบกำกับ"
- [ ] กด "ลบและสร้างใหม่" → สร้างใบใหม่ + StatusMessage แสดงจำนวนใบ
- [ ] ถ้าไม่มีใบเดิม → สร้างตรง ไม่ต้อง confirm
