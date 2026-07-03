# Backend Brief — Phase: Export to Excel

**วันที่:** 2026-07-03
**Branch:** `feature/phase-export-excel` (สลับไปแล้ว ทำงานบน branch นี้)
**Feature ID:** `P-EXPORT-XLSX`

## Context (คุณคือใคร)
คุณคือ **backend_agent** ของทีม ETStock ทำงานบน working directory เดียวกับ repo บน branch `feature/phase-export-excel` (Lead สร้างไว้ให้แล้ว — อย่าสร้าง branch เอง, อย่า push, อย่าเปิด PR เอง).

## Read these files first
1. `.team/plan-export-excel.md` — แผนงาน phase นี้ (คอลัมน์ Excel, สถาปัตยกรรม)
2. `ETStock/Services/ExcelImportService.cs` — ตัวอย่างการใช้ ClosedXML (เครื่องมือเดียวกัน ใช้ `XLWorkbook`)
3. `ETStock/Services/IExcelImportService.cs` — pattern ของ service interface
4. `ETStock/ViewModels/MonthlyStockViewModels.cs` — ไฟล์หลักที่ต้องแก้ (ProductViewModel + MonthlyStockRowViewModel)
5. `ETStock/Program.cs` — ดูวิธี register DI
6. `ETStock/Views/ProductView.axaml.cs` — pattern การเปิด dialog ผ่าน event + TaskCompletionSource

## Scope (แก้ไฟล์เหล่านี้เท่านั้น)
1. `ETStock/Services/IExcelExportService.cs` **(สร้างใหม่)**
2. `ETStock/Services/ExcelExportService.cs` **(สร้างใหม่)**
3. `ETStock/Program.cs` — เพิ่ม DI registration
4. `ETStock/ViewModels/MonthlyStockViewModels.cs` — เพิ่ม property + command + event

## Task

### 1. สร้าง `IExcelExportService` + `ExcelExportService`
สร้าง interface และ implementation ที่ทำหน้าที่เขียนไฟล์ Excel จากข้อมูลในตาราง MonthlyStock

**`IExcelExportService.cs`:**
```csharp
using ETStock.ViewModels;

namespace ETStock.Services;

public interface IExcelExportService
{
    void WriteStock(IReadOnlyList<MonthlyStockRowViewModel> rows, int year, int month, string filePath);
}
```

**`ExcelExportService.cs`:**
- ใช้ ClosedXML (`XLWorkbook`) — pattern เดียวกับ ExcelImportService
- สร้าง worksheet ชื่อ `Stock_{year}_{month:00}`
- **แถวหัวตาราง (row 1) — ชื่อคอลัมน์ตามนี้ (ตาม PO ระบุ):**

| col | header | ข้อมูลจาก MonthlyStockRowViewModel |
|-----|--------|-------------------------------------|
| 1 | ลำดับ | `LineNumber` |
| 2 | ชื่อสินค้า | `Name` |
| 3 | ยอดยกมา | `OpeningQty` |
| 4 | ซื้อเข้า | `BuyQty` |
| 5 | ขายออก | `SalesQty` (computed: SellFullQty + SellPosQty) |
| 6 | บิลเต็ม | `SellFullQty` |
| 7 | คงเหลือ | `ClosingQty` (computed) |
| 8 | ราคาขาย | `SellPrice` |
| 9 | จำนวนเงิน | `SalesAmount` (computed: SalesQty × SellPrice) |
| 10 | ต้นทุนสินค้า | `CostPrice` |
| 11 | ต้นทุนรวม | `TotalCost` (computed: SalesQty × CostPrice — **ใหม่ ดู task 3**) |
| 12 | มูลค่าสินค้าคงเหลือ | `ClosingValue` (computed: ClosingQty × CostPrice) |

- แถวหัวตาราง: **Bold**, พื้นหลังสีอ่อน (เช่น `XLColor.LightGray`)
- Auto-fit columns (`ws.Columns().AdjustToContents()`)
- คอลัมน์ตัวเลข: จัดรูปแบบ 2 ทศนิยม (NumberFormat `"#,##0.00"`) สำหรับเซลล์ตัวเลขในคอลัมน์ 3-12 (ยกเว้น "ลำดับ" และ "ชื่อสินค้า")

### 2. Register DI ใน `Program.cs`
เพิ่มบรรทัด (หลัง ExcelImportService):
```csharp
services.AddSingleton<IExcelExportService, ExcelExportService>();
```

### 3. แก้ `MonthlyStockViewModels.cs`

#### 3a. MonthlyStockRowViewModel — เพิ่ม `TotalCost` computed property
เพิ่ม ณ ตำแหน่งใกล้ property คำนวณอื่น ๆ (ClosingQty, SalesQty, SalesAmount):
```csharp
public decimal TotalCost => SalesQty * CostPrice;
```
> `SalesQty` = SellFullQty + SellPosQty (มีอยู่แล้ว)

#### 3b. ProductViewModel — เพิ่ม export command + event
เพิ่ม field, constructor wiring และ command ตาม pattern เดียวกับ `ImportFromExcelAsync`:

- Field: `private TaskCompletionSource<string?>? _exportExcelTcs;`
- Event: `public event EventHandler? ExportExcelRequested;`
- Method: `public void CompleteExportExcel(string? path) => _exportExcelTcs?.TrySetResult(path);`
- Constructor ที่รับ `IExcelExportService` ใหม่ (เพิ่ม overload เดียวที่สมบูรณ์ที่สุด — 4 deps — เก็บ constructor เดิมไว้ทั้งหมด, เพิ่มอันใหม่ที่รับครบ):
```csharp
public ProductViewModel(
    IMonthlyStockRepository repository,
    IInvoiceGeneratorService invoiceGenerator,
    IExcelImportService excelImporter,
    IExcelExportService excelExporter,
    int year,
    int month)
    : this(repository, invoiceGenerator, excelImporter, year, month)
{
    _excelExporter = excelExporter;
}
```
- Field: `private readonly IExcelExportService? _excelExporter;`

- Command (pattern เหมือน GenerateInvoices + ImportFromExcel):
```csharp
[RelayCommand]
private async Task ExportExcelAsync()
{
    if (_excelExporter is null) { StatusMessage = "Excel export service is not available."; return; }
    if (!CanRun()) return;
    if (IsBusy) return;

    IsBusy = true;
    StatusMessage = string.Empty;

    try
    {
        // Step 1: auto-save ถ้ามี unsaved changes (pattern เดียวกับ GenerateInvoices)
        if (_hasUnsavedChanges)
        {
            StatusMessage = "กำลังบันทึกข้อมูลสต๊อก...";
            foreach (var row in Rows)
                await _repository!.SaveAsync(row.ToInput(SelectedYear, SelectedMonth));
            await LoadRowsAsync();
            StatusMessage = string.Empty;
        }

        // Step 2: เปิด SaveFileDialog (ผ่าน event → View code-behind)
        _exportExcelTcs = new TaskCompletionSource<string?>();
        ExportExcelRequested?.Invoke(this, EventArgs.Empty);
        var filePath = await _exportExcelTcs.Task;
        _exportExcelTcs = null;

        if (string.IsNullOrEmpty(filePath))
        {
            StatusMessage = "ยกเลิกการส่งออก";
            return;
        }

        // Step 3: เขียนไฟล์ Excel
        _excelExporter.WriteStock(Rows, SelectedYear, SelectedMonth, filePath);

        StatusMessage = "ส่งออกข้อมูลสำเร็จ";
    }
    catch (Exception ex)
    {
        StatusMessage = $"ไม่สามารถส่งออกข้อมูลได้: {ex.Message}";
    }
    finally
    {
        IsBusy = false;
    }
}
```

## Acceptance criteria
- [ ] สร้าง `IExcelExportService.cs` + `ExcelExportService.cs` ครบ
- [ ] `Program.cs` register DI แล้ว
- [ ] `MonthlyStockRowViewModel.TotalCost` computed property ทำงานถูกต้อง
- [ ] `ProductViewModel` มี `ExportExcelCommand` + `ExportExcelRequested` event + `CompleteExportExcel` method
- [ ] constructor ใหม่รับ `IExcelExportService` ครบ โดย constructor เดิมยังคงทำงานได้
- [ ] **`dotnet build` ผ่าน ไม่มี error**
- [ ] commit ด้วย prefix `feat(be):` และระบุ `[phase export-excel]` ในข้อความ

## หมายเหตุ — สัญญา interface สำหรับ FE (ต้องเขียนด้วย)
หลังเสร็จงาน ให้เขียนไฟล์ `.team/backend-contract-export-excel.md` ระบุ:
- ชื่อ command: `ExportExcelCommand`
- ชื่อ event: `ExportExcelRequested` (event pattern: VM เป็น publisher, View สมัครรับแล้วเปิด SaveFileDialog แล้วเรียก `CompleteExportExcel(path)`)
- method รับคืนค่า: `CompleteExportExcel(string? path)`
- ชื่อ default filename: `Stock_{year}_{month:00}.xlsx` (FE เป็นคนตั้ง default ใน SaveFilePicker)

## Git
- คุณ commit ในเครื่องเท่านั้น (`git add -A && git commit -m "feat(be): ... [phase export-excel]"`)
- **ห้าม push, ห้ามเปิด PR** — Lead จะทำให้
