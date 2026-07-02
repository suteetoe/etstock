# Frontend Brief — P-PRINT-ALL: Print All Invoices Button + Preview

## Context
- **คุณคือ:** Frontend Agent ของทีม ETStock
- **Branch:** `feature/phase-print-all-invoices` (อยู่ใน branch นี้อยู่แล้ว — เช็ค `git branch --show-current`)
- **Task ID:** P-PRINT-ALL-F
- **เป้าหมาย:** เพิ่มปุ่ม "พิมพ์ทั้งหมด" ใน InvoiceView ข้างปุ่ม "โหลด" เมื่อกด → เรียก GenerateAllPdfAsync → เปิด PdfPreviewWindow ดู PDF หลายหน้า ถ้าไม่มี invoice แจ้งเตือน

## Read these files first
1. `.team/backend-contract-print-all.md` — **สัญญา interface ใหม่** (GenerateAllPdfAsync)
2. `ETStock/ViewModels/InvoiceViewModel.cs` — ViewModel ปัจจุบัน (มี PrintAsync, PdfPreviewRequested event)
3. `ETStock/Views/InvoiceView.axaml` — View ปัจจุบมมีปุ่ม "โหลด" และ "พิมพ์" ต่อแถว)
4. `ETStock/Views/PdfPreviewWindow.axaml.cs` — รับ `byte[]` pdfBytes แสดง preview + ปุ่มพิมพ์ (รองรับหลายหน้าอยู่แล้ว)
5. `ETStock/Views/InvoiceView.axaml.cs` — ฟัง `PdfPreviewRequested` event → เปิด PdfPreviewWindow

## Scope (แก้ไฟล์เหล่านี้เท่านั้น)
- `ETStock/ViewModels/InvoiceViewModel.cs`
- `ETStock/Views/InvoiceView.axaml`

**ห้ามแก้:** Services, Repositories, Models, Tests, Views/PdfPreviewWindow*

## Task

### 1. เพิ่ม `PrintAllCommand` ใน `InvoiceViewModel`
วางใกล้กับ `PrintAsync` ที่มีอยู่ (ใช้ pattern เดียวกัน):

```csharp
[RelayCommand]
private async Task PrintAllAsync()
{
    if (_printService is null)
    {
        StatusMessage = "ไม่สามารถพิมพ์ได้: บริการพิมพ์ไม่พร้อม";
        return;
    }

    IsBusy = true;
    StatusMessage = string.Empty;
    try
    {
        var pdfBytes = await _printService.GenerateAllPdfAsync(SelectedYear, SelectedMonth);

        if (pdfBytes.Length == 0)
        {
            StatusMessage = "ไม่พบข้อมูลใบกำกับภาษีในงวดนั้น";
            return;
        }

        PdfPreviewRequested?.Invoke(pdfBytes);
        StatusMessage = $"สร้าง PDF ทั้งหมดแล้ว สำหรับงวด {SelectedMonth}/{SelectedYear}";
    }
    catch (Exception ex)
    {
        StatusMessage = $"ไม่สามารถสร้าง PDF ได้: {ex.Message}";
    }
    finally
    {
        IsBusy = false;
    }
}
```

**สำคัญ:** ใช้ event `PdfPreviewRequested` เดิม (ไม่ต้องสร้าง event ใหม่) InvoiceView.axaml.cs ฟัง event นี้อยู่แล้ว → เปิด PdfPreviewWindow ให้อัตโนมัติ

### 2. เพิ่มปุ่ม "พิมพ์ทั้งหมด" ใน `InvoiceView.axaml`
ในแถบเดียวกับปุ่ม "โหลด" (StackPanel Orientation="Horizontal" แถวแรก):

```xml
<Button Content="พิมพ์ทั้งหมด"
        Command="{Binding PrintAllCommand}"
        IsEnabled="{Binding !IsBusy}"/>
```

วางหลังปุ่ม "โหลด" (ก่อน TextBlock "เล่มที่ล่าสุด")

### 3. ไม่ต้องแก้อะไรเพิ่ม
- `PdfPreviewWindow` รองรับ PDF หลายหน้าอยู่แล้ว (MuPDFCore)
- `InvoiceView.axaml.cs` ฟัง `PdfPreviewRequested` อยู่แล้ว
- DI ไม่ต้องเพิ่ม (ใช้ `IInvoicePrintService` เดิม)

## Acceptance Criteria
- [ ] มีปุ่ม "พิมพ์ทั้งหมด" ข้างปุ่ม "โหลด"
- [ ] กดปุ่ม → สร้าง PDF รวมทุกใบ → เปิด PdfPreviewWindow
- [ ] ไม่มี invoice → StatusMessage = "ไม่พบข้อมูลใบกำกับภาษีในงวดนั้น"
- [ ] IsBusy ทำงานระหว่างสร้าง PDF
- [ ] Build ผ่าน: `dotnet build ETStock.slnx` 0 errors
- [ ] commit ด้วย prefix `feat(fe):`

## วิธี commit
```
git add -A
git commit -m "feat(fe): add Print All button for multi-invoice PDF preview [phase print-all]"
```
