# QA Brief — Phase: Export to Excel

**วันที่:** 2026-07-03
**Branch:** `feature/phase-export-excel` (BE + FE commit แล้ว)
**Feature ID:** `P-EXPORT-XLSX`

## Context (คุณคือใคร)
คุณคือ **qa_agent** ของทีม ETStock ทำงานบน working directory เดียวกับ repo บน branch `feature/phase-export-excel`
(BE และ FE ได้ commit งานเสร็จแล้วบน branch นี้). **ห้ามแก้ production code — เฉพาะเขียน/รัน test.**
**ห้ามสร้าง branch, ห้าม push, ห้ามเปิด PR เอง.**

## Read these files first
1. `.team/plan-export-excel.md` — แผนงาน phase นี้
2. `.team/backend-brief-export-excel.md` — spec ของงาน BE
3. `.team/backend-contract-export-excel.md` — สัญญา interface
4. `.team/frontend-brief-export-excel.md` — spec ของงาน FE
5. `ETStock/Services/ExcelExportService.cs` — implementation ที่ต้อง test
6. `ETStock/ViewModels/MonthlyStockViewModels.cs` — ExportExcelCommand + TotalCost property
7. `ETStock.Tests/MonthlyStockViewModelTests.cs` — pattern test เดิม (ดูวิธี mock repository, สร้าง row, etc.)

## Scope (แก้ไฟล์เหล่านี้เท่านั้น)
1. `ETStock.Tests/ExcelExportServiceTests.cs` **(สร้างใหม่)**
2. `ETStock.Tests/MonthlyStockViewModelTests.cs` — เพิ่ม test cases สำหรับ ExportExcelCommand และ TotalCost

## Task

### 1. `ExcelExportServiceTests.cs` — Unit test ExcelExportService
สร้าง test class ที่ตรวจสอบว่าไฟล์ที่ส่งออกมี:
- ครบ 12 คอลัมน์ตาม header ที่ PO ระบุ
- header row เป็น bold + สีพื้นหลัง
- ข้อมูลแต่ละ row ถูกต้องตาม computed properties (SalesQty, ClosingQty, SalesAmount, TotalCost, ClosingValue)
- NumberFormat ของคอลัมน์ตัวเลขเป็น `#,##0.00`

**วิธี test (ไม่ต้อง mock):**
- สร้าง `ExcelExportService` instance ตรง ๆ (ไม่มี dependency)
- สร้าง `MonthlyStockRowViewModel` ผ่าน constructor `ExcelImportRecord` หรือ `ProductWithStock` (ดู test เดิมใน MonthlyStockViewModelTests ว่าสร้างอย่างไร)
- เขียนไฟล์ลง temp path (`Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.xlsx")`)
- เปิดไฟล์ด้วย ClosedXML (`XLWorkbook`) อ่านค่ากลับมาตรวจ
- ลบไฟล์ temp หลัง test (`File.Delete`)

**Test cases ขั้นต่ำ:**
- `WriteStock_CreatesFileWithCorrectHeaders` — ตรวจ 12 headers ตาม PO
- `WriteStock_WritesCorrectRowData` — ตรวจค่าทุกคอลัมน์ของ row แรก
- `WriteStock_TotalCostIsSalesQtyTimesCostPrice` — ตั้งค่าแล้วตรวจ TotalCost
- `WriteStock_AppliesNumberFormat` — ตรวจ NumberFormat ของเซลล์ตัวเลข

### 2. `MonthlyStockViewModelTests.cs` — เพิ่ม test cases สำหรับ ExportExcelCommand
**Pattern:** ดู test เดิมของ `GenerateInvoices` ในไฟล์เดียวกัน — ใช้ `FakeMonthlyStockRepository`, mock `IExcelExportService`, ใช้ `ExportExcelRequested` event + `CompleteExportExcel` เพื่อ simulate user

**Test cases ขั้นต่ำ:**
- `ExportExcel_SavesUnsavedChangesBeforeExport` — ตั้ง `_hasUnsavedChanges` (แก้ row) → export → ตรวจว่า repository.SaveAsync ถูกเรียก
- `ExportExcel_WritesFileWhenPathProvided` — ส่ง path ผ่าน CompleteExportExcel → ตรวจว่า IExcelExportService.WriteStock ถูกเรียก
- `ExportExcel_CancelDoesNotExport` — ส่ง null → ตรวจว่า WriteStock **ไม่** ถูกเรียก
- `TotalCost_EqualsSalesQtyTimesCostPrice` — ตรวจ computed property

## Mocking pattern
- สร้าง `FakeExcelExportService` หรือใช้ `NSubstitute`/`Moq` (ดูว่า test เดิมใช้อะไร — ถ้าไม่มี lib ให้สร้าง fake manual แบบเดียวกับ `FakeMonthlyStockRepository`)
- `FakeExcelExportService` เก็บล่าสุด `(rows, year, month, filePath)` ที่รับ เพื่อให้ test ตรวจได้

## Acceptance criteria
- [ ] `dotnet build` ผ่าน
- [ ] `dotnet test` — test ใหม่ทั้งหมดผ่าน
- [ ] **ไม่แก้ production code** (เฉพาะ file test เท่านั้น)
- [ ] coverage: ExportExcelCommand (4 cases) + ExcelExportService (4 cases) ขั้นต่ำ
- [ ] commit ด้วย prefix `test(qa):` และระบุ `[phase export-excel]` ในข้อความ
- [ ] เขียนรายงาน `.team/qa-report-export-excel.md` พร้อม verdict (PASS/FAIL), จำนวน test ผ่าน/รวม

## Git
- คุณ commit ในเครื่องเท่านั้น (`git add -A && git commit -m "test(qa): ..."`)
- **ห้าม push, ห้ามเปิด PR** — Lead จะทำให้
