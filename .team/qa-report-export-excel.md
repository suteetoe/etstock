# QA Report — Phase: Export to Excel

**วันที่:** 2026-07-03
**Branch:** `feature/phase-export-excel`
**Feature ID:** `P-EXPORT-XLSX`
**QA Commit:** `a4dbb1e`

---

## Verdict: ✅ PASS

---

## Test Results Summary

| | จำนวน |
|---|---|
| Tests ใหม่ (phase นี้) | **8** |
| ผ่าน (ใหม่) | **8** |
| ไม่ผ่าน (ใหม่) | **0** |
| Tests รวมทั้งหมด | 107 |
| ผ่านรวม | 103 |
| ไม่ผ่านรวม | 3 (pre-existing ทั้งหมด — ดูด้านล่าง) |
| ข้าม (skipped) | 1 |

---

## Tests ใหม่ (phase export-excel) — ทั้งหมดผ่าน

### `ExcelExportServiceTests` (ไฟล์ใหม่)

| Test | ผล |
|---|---|
| `WriteStock_CreatesFileWithCorrectHeaders` | ✅ PASS |
| `WriteStock_WritesCorrectRowData` | ✅ PASS |
| `WriteStock_TotalCostIsSalesQtyTimesCostPrice` | ✅ PASS |
| `WriteStock_AppliesNumberFormat` | ✅ PASS |

**วิธี test:** สร้าง `ExcelExportService` ตรง ๆ → เขียนไฟล์ลง temp path → เปิดซ้ำด้วย `XLWorkbook` → ตรวจค่า → ลบไฟล์

### `MonthlyStockViewModelTests` (เพิ่มเข้าไฟล์เดิม)

| Test | ผล |
|---|---|
| `TotalCost_EqualsSalesQtyTimesCostPrice` | ✅ PASS |
| `ExportExcel_WritesFileWhenPathProvided` | ✅ PASS |
| `ExportExcel_CancelDoesNotExport` | ✅ PASS |
| `ExportExcel_SavesUnsavedChangesBeforeExport` | ✅ PASS |

**Mocking pattern:** `FakeExcelExportService` (manual fake เก็บ call count + last args), `FakeExcelImportService` (returns empty list), ใช้ `ExportExcelRequested` event + `CompleteExportExcel` เพื่อ simulate user เลือก/ยกเลิก path

---

## Pre-existing Failures (ไม่เกี่ยวกับ phase นี้)

ทั้ง 3 รายการนี้ fail ก่อนที่ phase export-excel จะเริ่มต้น และไม่มีโค้ดในงานนี้แตะไฟล์เหล่านั้น:

| Test | สาเหตุที่คาดว่า fail |
|---|---|
| `InvoiceViewModelTests.DefaultYear_IsThaiYear` | ทดสอบว่า default year = 2569 (Buddhist Era) แต่ได้ 2026 (Gregorian) — logic ไม่ได้แปลงปี |
| `InvoicePrintServiceTests.BuildAsync_UsesDefaultCompany_WhenCompanyIsNull` | pre-existing bug ในส่วน invoice print (ไม่เกี่ยวกับ export excel) |
| `MonthlyStockRepositoryTests.CarryForwardAsync_UpdatesOpeningWithoutReplacingCurrentTransactions` | pre-existing test ใน repository layer (ไม่เกี่ยวกับ export excel) |

---

## Coverage ตาม Acceptance Criteria

| เกณฑ์ | สถานะ |
|---|---|
| `ExcelExportService` — 4 cases ขั้นต่ำ | ✅ ครบ (4/4) |
| `ExportExcelCommand` — 4 cases ขั้นต่ำ | ✅ ครบ (4/4) |
| `TotalCost` computed property | ✅ ครอบคลุม |
| `dotnet build` ผ่าน | ✅ |
| `dotnet test` — test ใหม่ทั้งหมดผ่าน | ✅ |
| ไม่แก้ production code | ✅ (แก้เฉพาะ `ETStock.Tests/`) |
| commit prefix `test(qa):` + `[phase export-excel]` | ✅ commit `a4dbb1e` |

---

## ไฟล์ที่แก้ไข

| ไฟล์ | การเปลี่ยนแปลง |
|---|---|
| `ETStock.Tests/ETStock.Tests.csproj` | เพิ่ม `ClosedXML 0.105.0` package reference |
| `ETStock.Tests/ExcelExportServiceTests.cs` | สร้างใหม่ — 4 test cases |
| `ETStock.Tests/MonthlyStockViewModelTests.cs` | เพิ่ม 4 test cases + `FakeExcelExportService` + `FakeExcelImportService` + `CreateViewModelWithExporter` helper |
