# Frontend Brief — Phase: Export to Excel

**วันที่:** 2026-07-03
**Branch:** `feature/phase-export-excel` (สลับไปแล้ว — BE commit แล้ว)
**Feature ID:** `P-EXPORT-XLSX`

## Context (คุณคือใคร)
คุณคือ **frontend_agent** ของทีม ETStock ทำงานบน working directory เดียวกับ repo บน branch `feature/phase-export-excel`
(BE ได้ commit งานเสร็จแล้วบน branch นี้ — คุณทำต่อ). **ห้ามสร้าง branch เอง, ห้าม push, ห้ามเปิด PR เอง.**

## Read these files first
1. `.team/plan-export-excel.md` — แผนงาน phase นี้
2. `.team/backend-contract-export-excel.md` — สัญญา interface ที่ BE สร้างไว้
3. `ETStock/Views/ProductView.axaml` — ไฟล์ที่ต้องแก้ (เพิ่มปุ่ม)
4. `ETStock/Views/ProductView.axaml.cs` — ไฟล์ที่ต้องแก้ (event handler เปิด SaveFileDialog)
5. `ETStock/ViewModels/MonthlyStockViewModels.cs` — ดู ExportExcelRequested event + CompleteExportExcel method ที่ BE เพิ่ม
6. `ETStock/ViewModels/MainWindowViewModel.cs` — ดูว่าต้องอัปเดต ProductViewModel creation หรือไม่ (เพิ่ม dependency ใหม่)

## Scope (แก้ไฟล์เหล่านี้เท่านั้น)
1. `ETStock/Views/ProductView.axaml`
2. `ETStock/Views/ProductView.axaml.cs`
3. `ETStock/ViewModels/MainWindowViewModel.cs` — (ถ้า BE บอกว่าต้องอัปเดตการสร้าง ProductViewModel ด้วย dependency ใหม่)

## Task

### 1. เพิ่มปุ่ม "ส่งออกเป็น Excel" ใน `ProductView.axaml`
เพิ่มปุ่มใน StackPanel (Grid.Row="1") ข้างปุ่ม "Save" และ "สร้างใบกำกับภาษีอย่างย่อ":
```xml
<Button Content="ส่งออกเป็น Excel"
        Command="{Binding ExportExcelCommand}"
        IsEnabled="{Binding !IsBusy}"/>
```

### 2. เพิ่ม Event Handler สำหรับ SaveFileDialog ใน `ProductView.axaml.cs`
**ใช้ Avalonia 11 Storage API** (`StorageProvider.SaveFilePickerAsync`) ไม่ใช้ OpenFileDialog รุ่นเก่า

เพิ่มใน code-behind:
- ใน `OnDataContextChanged`: subscribe `_vm.ExportExcelRequested += OnExportExcelRequested;` และ unsubscribe ตัวเก่า
- Implement handler:
```csharp
private async void OnExportExcelRequested(object? sender, EventArgs e)
{
    var topLevel = TopLevel.GetTopLevel(this) as Window;
    if (topLevel is null || _vm is null) { _vm?.CompleteExportExcel(null); return; }

    var year = _vm.SelectedYear;
    var month = _vm.SelectedMonth;
    var defaultName = $"Stock_{year}_{month:00}.xlsx";

    var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
    {
        Title = "บันทึกไฟล์ Excel",
        SuggestedFileName = defaultName,
        DefaultExtension = "xlsx",
        FileTypeChoices = new[]
        {
            new Avalonia.Platform.Storage.FilePickerFileType("Excel")
            {
                Patterns = new[] { "*.xlsx" }
            }
        }
    });

    _vm.CompleteExportExcel(file?.Path.LocalPath);
}
```

> **สำคัญ:** `file?.Path.LocalPath` คือ local filesystem path ที่ BE ต้องการ (ส่งให้ ClosedXML)
> ถ้า user ยกเลิก → `file` เป็น null → ส่ง `null` ให้ CompleteExportExcel

### 3. ตรวจ `MainWindowViewModel.cs`
ดูว่า BE ได้แก้การสร้าง ProductViewModel ใน MainWindowViewModel แล้วหรือยัง (เพิ่ม `IExcelExportService` dependency)
ถ้ายัง — เพิ่มเองโดยดึงจาก `App.Services` pattern ที่ใช้อยู่ ถ้า BE แก้แล้ว — ข้ามได้

## Acceptance criteria
- [ ] ปุ่ม "ส่งออกเป็น Excel" แสดงบน ProductView ข้างปุ่ม Save
- [ ] กดปุ่มแล้วเปิด SaveFilePicker (ไม่ใช่ OpenFileDialog)
- [ ] default filename = `Stock_{year}_{month:00}.xlsx`
- [ ] DefaultExtension = "xlsx", กรองเฉพาะไฟล์ .xlsx
- [ ] เลือกไฟล์แล้วส่ง local path ให้ CompleteExportExcel
- [ ] ยกเลิกแล้วส่ง null ให้ CompleteExportExcel
- [ ] **`dotnet build` ผ่าน ไม่มี error**
- [ ] ไม่มี logic ใน code-behind นอกจากเปิด SaveFilePicker (MVVM)
- [ ] commit ด้วย prefix `feat(fe):` และระบุ `[phase export-excel]` ในข้อความ

## Git
- คุณ commit ในเครื่องเท่านั้น (`git add -A && git commit -m "feat(fe): ..."`)
- **ห้าม push, ห้ามเปิด PR** — Lead จะทำให้
