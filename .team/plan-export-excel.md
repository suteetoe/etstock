# Plan — Phase: Export to Excel

**สร้างเมื่อ:** 2026-07-03
**Branch:** `feature/phase-export-excel`
**Base:** `develop`
**Feature ID:** `P-EXPORT-XLSX`

## เป้าหมาย
เพิ่มฟีเจอร์ "ส่งออกเป็น Excel" บนหน้า Monthly Stock Transactions:
1. เพิ่มปุ่ม "ส่งออกเป็น Excel"
2. ตรวจสอบการบันทึกข้อมูล — หากมีข้อมูลที่ยังไม่ได้บันทึกให้ Save ก่อน
3. เปิด SaveFileDialog ให้ user ระบุตำแหน่ง/ชื่อไฟล์ (default: `Stock_ปี_เดือน.xlsx`)
4. ส่งออกข้อมูลจากตารางเป็น Excel ตามคอลัมน์ที่กำหนด
5. แจ้งเตือนเมื่อส่งออกสำเร็จ

## คอลัมน์ Excel (ตาม PO)
| # | ชื่อคอลัมน์ | ที่มะ (Row ViewModel) |
|---|-----------|---------------------|
| 1 | ลำดับ | `LineNumber` |
| 2 | ชื่อสินค้า | `Name` |
| 3 | ยอดยกมา | `OpeningQty` |
| 4 | ซื้อเข้า | `BuyQty` |
| 5 | ขายออก | `SalesQty` (= SellFullQty + SellPosQty) |
| 6 | บิลเต็ม | `SellFullQty` |
| 7 | คงเหลือ | `ClosingQty` |
| 8 | ราคาขาย | `SellPrice` |
| 9 | จำนวนเงิน | `SalesAmount` (= SalesQty × SellPrice) |
| 10 | ต้นทุนสินค้า | `CostPrice` |
| 11 | ต้นทุนรวม | `TotalCost` (= SalesQty × CostPrice) — **ใหม่ ต้องเพิ่ม computed property** |
| 12 | มูลค่าสินค้าคงเหลือ | `ClosingValue` (= ClosingQty × CostPrice) |

> **หมายเหตุ:** PO ระบุ "ขายออก" และ "บิลเต็ม" เป็นคนละคอลัมน์
> - "ขายออก" = ปริมาณขายรวม (SellFullQty + SellPosQty)
> - "บิลเต็ม" = SellFullQty (จำนวนที่ออกบิลเต็ม)
> - คอลัมน์ "ต้นทุนรวม" = ต้นทุนสินค้าที่ขาย = SalesQty × CostPrice (ยังไม่มีใน ViewModel → ต้องเพิ่ม)

## สถาปัตยกรรมที่เลือก
- ใช้ **ClosedXML** (มีอยู่ใน csproj แล้ว จาก ExcelImportService) — ไม่ต้องเพิ่ม package
- สร้าง **`IExcelExportService` + `ExcelExportService`** ใน `ETStock/Services/` ทำหน้าที่รับ `IEnumerable<MonthlyStockRowViewModel>` (หรือ DTO) + year/month → เขียนไฟล์ .xlsx
- ViewModel เพิ่ม `ExportExcelCommand`:
  1. เช็ค `_hasUnsavedChanges` → เรียก Save ก่อน (reuse pattern เดียวกับ GenerateInvoices)
  2. เปิด SaveFileDialog ผ่าน event + TaskCompletionSource (pattern เดียวกับ ImportExcel)
  3. เรียก `IExcelExportService.WriteAsync(rows, path)`
  4. StatusMessage = "ส่งออกข้อมูลสำเร็จ"
- View (code-behind) ติดตั้ง event handler เปิด `StorageProvider.SaveFilePickerAsync` (Avalonia 11 API) พร้อม default filename `Stock_{year}_{month:00}.xlsx`

## Tasks
| Task | Group | Scope (ไฟล์ที่แก้ได้) |
|------|-------|----------------------|
| 1 | Backend | `ETStock/Services/IExcelExportService.cs` (ใหม่), `ETStock/Services/ExcelExportService.cs` (ใหม่), `ETStock/Program.cs` (DI register), `ETStock/ViewModels/MonthlyStockViewModels.cs` (เพิ่ม `TotalCost` computed property ใน Row VM + เพิ่ม `ExportExcelCommand` + `ExportExcelRequested` event + `CompleteExportExcel` ใน ProductViewModel) |
| 2 | Frontend | `ETStock/Views/ProductView.axaml` (เพิ่มปุ่ม), `ETStock/Views/ProductView.axaml.cs` (event handler เปิด SaveFilePicker) |
| 3 | QA | `ETStock.Tests/MonthlyStockViewModelTests.cs` (เพิ่ม test ExportExcel), `ETStock.Tests/ExcelExportServiceTests.cs` (ใหม่) |

## Dependency
1 → 2 → 3 (FE ต้องรอ interface จาก BE, QA ต้องรอสองส่วนเสร็จ)
แต่เนื่องจากทั้งสาม commit ลง branch เดียวกัน → รันตามลำดับ

## Definition of Done
- [ ] build ผ่าน (`dotnet build`) ไม่มี error
- [ ] ปุ่ม "ส่งออกเป็น Excel" แสดงบน ProductView
- [ ] กดปุ่มแล้ว: ถ้ามี unsaved changes → save ก่อน → เปิด SaveFileDialog → ส่งออก → แจ้งเตือนสำเร็จ
- [ ] คอลัมน์ Excel ครบ 12 คอลัมน์ตามที่ PO ระบุ
- [ ] default filename = `Stock_{year}_{month:00}.xlsx`
- [ ] มี unit test ครอบคลุม ExportExcelCommand + ExcelExportService
- [ ] UI ผูก ViewModel ตาม MVVM (ไม่มี logic ใน code-behind นอกจากเปิด dialog)
- [ ] QA verdict = PASS
- [ ] เปิด PR เข้า `develop`
