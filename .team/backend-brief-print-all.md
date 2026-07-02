# Backend Brief — P-PRINT-ALL: Print All Invoices as Multi-page PDF

## Context
- **คุณคือ:** Backend Agent ของทีม ETStock
- **Branch:** `feature/phase-print-all-invoices` (สร้างจาก develop แล้ว อยู่ใน branch นี้อยู่แล้ว)
- **Task ID:** P-PRINT-ALL-B
- **เป้าหมาย:** เพิ่มเมธอด `GenerateAllPdfAsync` ใน `IInvoicePrintService` เพื่อสร้าง PDF ไฟล์เดียวที่รวมทุกใบกำกับในงวด โดย **1 บิล = 1 หน้า**

## Read these files first
1. `ETStock/Services/IInvoicePrintService.cs` — interface ปัจจุบัน (มี BuildAsync + GeneratePdfAsync)
2. `ETStock/Services/InvoicePrintService.cs` — implementation ปัจจุบัน
3. `ETStock/Services/InvoicePdfDocument.cs` — QuestPDF document สำหรับ 1 invoice (class `internal sealed`)
4. `ETStock/Models/InvoiceDocumentModel.cs` — record model สำหรับ 1 invoice
5. `ETStock/Data/Repositories/IAbbrInvoiceRepository.cs` — มี `GetByPeriodAsync(int taxYear, int taxMonth)`
6. `.team/backend-contract-p6.md` — ตัวอย่าง contract format

## Scope (แก้ไฟล์เหล่านี้เท่านั้น)
- `ETStock/Services/IInvoicePrintService.cs`
- `ETStock/Services/InvoicePrintService.cs`
- `ETStock/Services/InvoicePdfDocument.cs` (เฉพาะถ้าจำเป็นต้อง refactor เพื่อรองรับ multi-page)
- `.team/backend-contract-print-all.md` (สร้างใหม่ — contract สำหรับ FE)

**ห้ามแก้:** ViewModels, Views, Tests (FE/QA จะทำ)

## Task

### 1. เพิ่มเมธอดใน `IInvoicePrintService`
```csharp
/// สร้าง PDF ที่รวมทุกใบกำกับในงวด (taxYear, taxMonth) — 1 บิล = 1 หน้า
/// คืน byte[] ของ PDF หรือคืน byte[] ว่างถ้าไม่มี invoice เลย
Task<byte[]> GenerateAllPdfAsync(int taxYear, int taxMonth, CancellationToken ct = default);
```

### 2. Implement ใน `InvoicePrintService`
- ดึง invoices ทั้งหมดในงวด: `var invoices = await _invoiceRepo.GetByPeriodAsync(taxYear, taxMonth);`
- ถ้า `invoices.Count == 0` → **คืน `Array.Empty<byte>()`** (FE จะตรวจเองว่าว่าง → แสดงข้อความเตือน)
- สำหรับแต่ละ invoice: เรียก `BuildAsync` (หรือ refactor BuildAsync ให้รับ `AbbrInvoice` entity ตรง ๆ เพื่อไม่ต้อง query ซ้ำ — **แนะนำ** ให้สร้าง overload หรือ extract private helper)
- ใช้ QuestPDF `Document.Create(container => { foreach doc: container.Page(page => ...) })` เพื่อสร้าง multi-page document

### 3. ทางเลือกสำหรับ multi-page (เลือกอย่างใดอย่างหนึ่ง)

**Option A (แนะนำ):** สร้าง class ใหม่ `MultiInvoicePdfDocument : IDocument` ที่รับ `IReadOnlyList<InvoiceDocumentModel>` แล้ววนลูป `container.Page()` สำหรับแต่ละใบ โดย compose logic เดียวกับ `InvoicePdfDocument` (อาจ extract section methods เป็น static/shared)

**Option B:** Refactor `InvoicePdfDocument` ให้รับ list แล้ววนลูป — แต่กระทบผู้ใช้เดิม (GeneratePdfAsync 1 ใบ) ต้องระวัง

**เลือก Option A** เพื่อไม่กระทบ single-invoice path

### 4. เขียน Backend Contract
สร้าง `.team/backend-contract-print-all.md` ระบุ:
- Method signature ใหม่
- พฤติกรรม (คืน empty array เมื่อไม่มี invoice)
- ตัวอย่างการเรียกจาก ViewModel
- ว่าไม่ต้องเพิ่ม DI registration ใหม่ (ใช้ `IInvoicePrintService` เดิม)

## Acceptance Criteria
- [ ] `IInvoicePrintService.GenerateAllPdfAsync(int, int, CancellationToken)` มีใน interface
- [ ] `InvoicePrintService` implement ครบ สร้าง PDF หลายหน้า 1 บิล/หน้า
- [ ] ถ้าไม่มี invoice → คืน `Array.Empty<byte>()`
- [ ] Build ผ่าน: `dotnet build ETStock.slnx` 0 errors
- [ ] PDF ที่สร้างมีหน้าเท่ากับจำนวน invoice
- [ ] commit ด้วย prefix `feat(be):` บน branch ปัจจุบัน
- [ ] เขียน `.team/backend-contract-print-all.md` ครบ

## DI ปัจจุบัน (ไม่ต้องแก้)
`IInvoicePrintService` ลงทะเบียนใน `Program.cs` เป็น `AddScoped` อยู่แล้ว

## วิธี commit
```
git add -A
git commit -m "feat(be): add GenerateAllPdfAsync for multi-invoice PDF"
```
