# QA Brief — P-PRINT-ALL: Tests for Print All Invoices

## Context
- **คุณคือ:** QA Agent ของทีม ETStock
- **Branch:** `feature/phase-print-all-invoices` (อยู่ใน branch นี้อยู่แล้ว — เช็ค `git branch --show-current`)
- **Task ID:** P-PRINT-ALL-Q
- **เป้าหมาย:** เขียน unit tests ครอบคลุม `GenerateAllPdfAsync` (Backend) และ `PrintAllCommand` (Frontend ViewModel)

## Read these files first
1. `.team/backend-contract-print-all.md` — สัญญา interface
2. `ETStock/Services/IInvoicePrintService.cs` — interface ใหม่มี `GenerateAllPdfAsync`
3. `ETStock/Services/InvoicePrintService.cs` — implementation
4. `ETStock/ViewModels/InvoiceViewModel.cs` — มี `PrintAllCommand` (PrintAllAsync method)
5. `ETStock.Tests/InvoicePrintServiceTests.cs` — **ไฟล์ที่จะเพิ่ม test** (มี tests เดิมของ GeneratePdfAsync)
6. `ETStock.Tests/InvoiceViewModelTests.cs` — **ไฟล์ที่จะเพิ่ม test** (มี tests เดิมของ PrintCommand)

## Scope (แก้ไฟล์เหล่านี้เท่านั้น)
- `ETStock.Tests/InvoicePrintServiceTests.cs`
- `ETStock.Tests/InvoiceViewModelTests.cs`

**ห้ามแก้ production code** (Services, ViewModels, Views)

## Task

### 1. Backend Tests — `InvoicePrintServiceTests.cs`
ศึกษา pattern ของ test เดิม (ใช้ Fake repos / in-memory) แล้วเพิ่ม:

**a) `GenerateAllPdfAsync_NoInvoices_ReturnsEmptyArray`**
- Setup: FakeAbbrInvoiceRepository ที่คืน empty list สำหรับงวดนั้น
- Assert: result.Length == 0

**b) `GenerateAllPdfAsync_OneInvoice_ReturnsNonEmptyPdf`**
- Setup: 1 invoice ในงวด
- Assert: result.Length > 0 (PDF header check: ผลลัพธ์ขึ้นต้นด้วย `%PDF`)

**c) `GenerateAllPdfAsync_MultipleInvoices_ReturnsMultiPagePdf`**
- Setup: 3 invoices ในงวด
- Assert: result.Length > 0, ขึ้นต้นด้วย `%PDF`
- (ถ้าเปิดได้ ตรวจจำนวนหน้าด้วย QuestPDF หรือ PDF parser — ถ้ายากให้ตรวจแค่ว่าเป็น PDF valid ก็พอ)

### 2. Frontend Tests — `InvoiceViewModelTests.cs`
ศึกษา pattern ของ test เดิม (ใช้ FakeRepository / mock printService) แล้วเพิ่ม:

**a) `PrintAllCommand_NoInvoices_SetsWarningStatus`**
- Setup: InvoiceViewModel ที่มี invoices เป็น empty (GenerateAllPdfAsync คืน empty)
- Act: PrintAllCommand.Execute(null)
- Assert: StatusMessage มีคำว่า "ไม่พบข้อมูลใบกำกับภาษีในงวดนั้น"

**b) `PrintAllCommand_HasInvoices_RaisesPdfPreviewRequested`**
- Setup: mock printService ที่คืน PDF bytes (non-empty), subscribe PdfPreviewRequested event
- Act: PrintAllCommand.Execute(null)
- Assert: event ถูกเรียก, ส่ง bytes ที่ non-empty

### 3. รัน build + test ทั้งหมด
```
dotnet build ETStock.slnx
dotnet test ETStock.slnx --no-build
```
รายงานผล: x/y ผ่าน, ระบุ failing tests (ถ้ามี — แยกว่าเกี่ยวกับงานนี้หรือ pre-existing)

## Acceptance Criteria
- [ ] เพิ่มอย่างน้อย 5 test facts ครอบคลุม GenerateAllPdfAsync + PrintAllCommand
- [ ] Build ผ่าน
- [ ] Test ใหม่ทั้งหมดผ่าน (failing ต้องเป็น pre-existing เท่านั้น)
- [ ] commit ด้วย prefix `test(qa):`

## วิธี commit
```
git add -A
git commit -m "test(qa): add tests for GenerateAllPdfAsync + PrintAllCommand [phase print-all]"
```
