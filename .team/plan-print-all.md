# Plan — Print All Abbreviated Invoices (Multi-page PDF)

- **Phase ID:** P-PRINT-ALL
- **Branch:** `feature/phase-print-all-invoices` (base: develop)
- **Created:** 2026-07-02

## เป้าหมาย
เพิ่มความสามารถ "พิมพ์ทั้งหมด" สำหรับใบกำกับภาษีอย่างย่อในงวด (ปี/เดือน) ที่เลือก:
1. ปุ่ม "พิมพ์ทั้งหมด" ในแถบเดียวกับปุ่ม "โหลด" ใน InvoiceView
2. สร้าง PDF ไฟล์เดียว หลายหน้า — 1 บิล = 1 หน้า วางต่อกัน
3. เปิด Preview ก่อนพิมพ์ (ใช้ PdfPreviewWindow เดิม)
4. ถ้างวดนั้นไม่มีใบกำกับเลย → แสดงข้อความเตือน "ไม่พบข้อมูลใบกำกับภาษีในงวดนั้น" และไม่สร้าง PDF

## งานแยกตามบทบาท (1 phase = 1 branch = 1 PR)

### Backend (P-PRINT-ALL-B)
- เพิ่มเมธอด `GenerateAllPdfAsync(int taxYear, int taxMonth, ct)` ใน `IInvoicePrintService`
- ดึงทุก invoice ในงวด → สร้าง InvoiceDocumentModel ต่อใบ → รวมเป็น PDF 1 ไฟล์ (1 invoice = 1 page)
- ใช้ QuestPDF `Document.Create(container => { foreach invoice: container.Page(...) })` หรือสร้าง multi-page document
- เขียน contract ใหม่ใน `.team/backend-contract-print-all.md`

### Frontend (P-PRINT-ALL-F) — รอ BE ส่งมอบ contract
- `InvoiceViewModel`: เพิ่ม `PrintAllCommand` (RelayCommand) + `PdfPreviewRequested` event (ใช้ event เดิม)
- `InvoiceView.axaml`: เพิ่มปุ่ม "พิมพ์ทั้งหมด" ข้างปุ่ม "โหลด"
- Logic: IsBusy, ถ้าไม่มี invoice → StatusMessage = "ไม่พบข้อมูลใบกำกับภาษีในงวดนั้น"
- ใช้ `PdfPreviewWindow` เดิมในการ preview PDF หลายหน้า (MuPDFCore รองรับอยู่แล้ว)

### QA (P-PRINT-ALL-Q) — รอ BE+FE
- เพิ่ม unit tests:
  - `InvoicePrintServiceTests.GenerateAllPdfAsync_*` — หลาย invoice, ไม่มี invoice, 1 invoice
  - `InvoiceViewModelTests.PrintAllCommand_*` — no invoices → status message, has invoices → event raised
- รัน build + test ทั้งหมด รายงาน verdict

## Dependency
BE → FE → QA (ทำตามลำดับ เพราะ FE ต้องใช้ contract ของ BE)

## Definition of Done
- [ ] build ผ่าน (`dotnet build`)
- [ ] test ผ่าน (`dotnet test`)
- [ ] ปุ่ม "พิมพ์ทั้งหมด" ทำงานได้ สร้าง PDF หลายหน้า
- [ ] ไม่มี invoice → แจ้งเตือน
- [ ] commit อยู่บน `feature/phase-print-all-invoices`
- [ ] QA verdict = PASS
- [ ] เปิด PR เข้า `develop`
