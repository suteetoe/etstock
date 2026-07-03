# PR — Phase: Export to Excel

## สรุป Phase
เพิ่มฟีเจอร์ **"ส่งออกเป็น Excel"** บนหน้า Monthly Stock Transactions ให้ user ส่งออกข้อมูลตารางสต๊อกรายเดือนเป็นไฟล์ `.xlsx`

## ลำดับการทำงานของฟีเจอร์
1. User กดปุ่ม **"ส่งออกเป็น Excel"**
2. ระบบตรวจสอบ — หากมีข้อมูลที่ยังไม่ได้บันทึกจะ **Save ก่อนอัตโนมัติ**
3. เปิด **SaveFileDialog** ให้ user ระบุตำแหน่ง/ชื่อไฟล์ (default: `Stock_{year}_{month:00}.xlsx`)
4. ส่งออกข้อมูลเป็น Excel ครบ **12 คอลัมน์**
5. แสดงข้อความ **"ส่งออกข้อมูลสำเร็จ"**

## รายการการเปลี่ยนแปลง

### Backend (`feat(be):`)
- **ใหม่** `ETStock/Services/IExcelExportService.cs` — interface สำหรับ export
- **ใหม่** `ETStock/Services/ExcelExportService.cs` — implementation ใช้ ClosedXML (มีอยู่แล้วใน csproj)
- `ETStock/Program.cs` — register `IExcelExportService` ใน DI
- `ETStock/ViewModels/MonthlyStockViewModels.cs`:
  - เพิ่ม `MonthlyStockRowViewModel.TotalCost` computed property (= SalesQty × CostPrice)
  - เพิ่ม `ExportExcelCommand` + `ExportExcelRequested` event + `CompleteExportExcel(string?)`
  - เพิ่ม constructor overload ที่รับ `IExcelExportService`
  - ตรวจ `_hasUnsavedChanges` → save ก่อน export (reuse pattern เดียวกับ GenerateInvoices)
- `ETStock/ViewModels/MainWindowViewModel.cs` — ส่ง `IExcelExportService` เข้า ProductViewModel

### Frontend (`feat(fe):`)
- `ETStock/Views/ProductView.axaml` — เพิ่มปุ่ม "ส่งออกเป็น Excel" (bind `ExportExcelCommand`)
- `ETStock/Views/ProductView.axaml.cs` — event handler `OnExportExcelRequested` เปิด `StorageProvider.SaveFilePickerAsync` (Avalonia 11 storage API)
  - default filename: `Stock_{year}_{month:00}.xlsx`
  - DefaultExtension = "xlsx", filter `*.xlsx`
  - ส่ง local path กลับ หรือ null ถ้า user ยกเลิก

### QA (`test(qa):`)
- **ใหม่** `ETStock.Tests/ExcelExportServiceTests.cs` — 4 test cases
- `ETStock.Tests/MonthlyStockViewModelTests.cs` — +4 test cases สำหรับ ExportExcelCommand + TotalCost

## คอลัมน์ Excel (12 คอลัมน์ตาม PO)
| # | คอลัมน์ | ที่มา |
|---|--------|------|
| 1 | ลำดับ | LineNumber |
| 2 | ชื่อสินค้า | Name |
| 3 | ยอดยกมา | OpeningQty |
| 4 | ซื้อเข้า | BuyQty |
| 5 | ขายออก | SalesQty (SellFullQty + SellPosQty) |
| 6 | บิลเต็ม | SellFullQty |
| 7 | คงเหลือ | ClosingQty |
| 8 | ราคาขาย | SellPrice |
| 9 | จำนวนเงิน | SalesAmount (SalesQty × SellPrice) |
| 10 | ต้นทุนสินค้า | CostPrice |
| 11 | ต้นทุนรวม | TotalCost (SalesQty × CostPrice) — **ใหม่** |
| 12 | มูลค่าสินค้าคงเหลือ | ClosingValue (ClosingQty × CostPrice) |

## Business rules ที่ครอบคลุม
- คงเหลือ = ยกมา + ซื้อ − ขาย(เต็ม) − ขายหน้าร้าน ✓ (ใช้ computed properties ที่ถูกต้อง)
- export ใช้ข้อมูลล่าสุดเสมอ (save ก่อน export ถ้ามี unsaved changes)

## ผลการทดสอบ
- **Build:** ผ่าน 0 error 0 warning
- **Unit tests:** 8/8 test ใหม่ผ่าน
  - ExcelExportServiceTests: 4/4
  - ExportExcelCommand tests: 4/4
- **Pre-existing failures (3 รายการ ไม่เกี่ยวกับ phase นี้):**
  - `InvoiceViewModelTests.DefaultYear_IsThaiYear`
  - `InvoicePrintServiceTests.BuildAsync_UsesDefaultCompany_WhenCompanyIsNull`
  - `MonthlyStockRepositoryTests.CarryForwardAsync_UpdatesOpeningWithoutReplacingCurrentTransactions`
- **QA verdict: PASS**

## Definition of Done
- [x] build ผ่าน (`dotnet build`) ไม่มี error
- [x] ปุ่ม "ส่งออกเป็น Excel" แสดงบน ProductView
- [x] กดปุ่ม → save ถ้ามี unsaved → เปิด SaveFileDialog → export → แจ้งเตือนสำเร็จ
- [x] คอลัมน์ Excel ครบ 12 คอลัมน์ตาม PO ระบุ
- [x] default filename = `Stock_{year}_{month:00}.xlsx`
- [x] unit test ครอบคลุม ExportExcelCommand + ExcelExportService
- [x] UI ผูก ViewModel ตาม MVVM (code-behind เฉพาะเปิด dialog)
- [x] QA verdict = PASS

## Dependencies
- ใช้ ClosedXML 0.105.0 (มีอยู่แล้วใน csproj — ไม่ต้องเพิ่ม package)
- ไม่มี migration ใหม่ (ไม่กระทบ DB)

## Phase ถัดไปที่แนะนำ
- ปรับแต่งรูปแบบ Excel (เพิ่ม header ชื่อบริษัท/เดือน-ปี, total row)
- ส่งออกใบกำกับภาษีอย่างย่อเป็น Excel
- เพิ่ม pre-existing failing tests ที่คั่งค้างกลับมาเขียว
