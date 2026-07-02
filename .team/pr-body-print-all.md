# [Phase Print All] พิมพ์ใบกำกับภาษีอย่างย่อทั้งหมด (Multi-page PDF)

## สรุป
เพิ่มความสามารถ "พิมพ์ทั้งหมด" สำหรับใบกำกับภาษีอย่างย่อในงวด (ปี/เดือน) ที่เลือก — สร้าง PDF ไฟล์เดียวหลายหน้า (1 บิล = 1 หน้า) พร้อม preview ก่อนพิมพ์

## การเปลี่ยนแปลงแยกตามบทบาท

### Backend (`feat(be):`)
- `IInvoicePrintService.cs` — เพิ่ม `GenerateAllPdfAsync(int taxYear, int taxMonth, CancellationToken)`
- `InvoicePrintService.cs` — implement: ดึงทุก invoice ในงวด → refactor `Build` เป็น private helper ใช้ร่วม → คืน `Array.Empty<byte>()` เมื่อไม่มี invoice
- `InvoicePdfDocument.cs` — เพิ่ม `MultiInvoicePdfDocument : IDocument` รับ `IReadOnlyList<InvoiceDocumentModel>` แล้ววนลูป `container.Page()` ใช้ layout เดิมของ `InvoicePdfDocument` (reuse Compose ทั้งหมด)

### Frontend (`feat(fe):`)
- `InvoiceViewModel.cs` — เพิ่ม `PrintAllCommand` (RelayCommand → `PrintAllAsync`) ใช้ event `PdfPreviewRequested` เดิม
- `InvoiceView.axaml` — เพิ่มปุ่ม "พิมพ์ทั้งหมด" ข้างปุ่ม "โหลด"

### QA (`test(qa):`)
- `InvoicePrintServiceTests.cs` — 4 facts ใหม่: no invoices → empty array, 1 invoice → PDF, 3 invoices → 3 pages, period filtering
- `InvoiceViewModelTests.cs` — 4 facts ใหม่: no invoices → warning, has invoices → raises event, passes period args, no print service → error

## Business rules ที่ครอบคลุม
- ✅ สร้าง PDF 1 บิลต่อ 1 หน้าตามที่ PO ระบุ
- ✅ ไม่มี invoice ในงวด → แสดง "ไม่พบข้อมูลใบกำกับภาษีในงวดนั้น" (ไม่สร้าง PDF)
- ✅ Preview ผ่าน PdfPreviewWindow เดิม (MuPDFCore รองรับหลายหน้า)

## ผลการทดสอบ
- **Build:** ผ่าน — 0 errors, 8 warnings (เป็น `System.IO.Packaging` vulnerability เดิม ไม่เกี่ยวข้อง)
- **Tests:** 83/85 ผ่าน
  - ✅ 8 facts ใหม่ของ phase นี้ผ่านครบ
  - ❌ 2 pre-existing failures (ไม่เกี่ยวกับงานนี้):
    - `DefaultYear_IsThaiYear` — hardcode ปีไทยใน test แต่ constructor ใช้คริสต์ศักราช (มาก่อน)
    - `CarryForwardAsync_UpdatesOpeningWithoutReplacingCurrentTransactions` — pre-existing ตั้งแต่ PR #28

## Definition of Done
- [x] ไฟล์ brief ครบทุก task ใน `.team/`
- [x] อยู่บน feature branch `feature/phase-print-all-invoices`
- [x] build ผ่าน
- [x] test ที่เกี่ยวข้องผ่าน (8/8 ใหม่)
- [x] UI ผูก ViewModel ตาม MVVM
- [x] QA verdict = PASS (8/8 new tests green; 2 failures pre-existing)

## Phase ถัดไปที่แนะนำ
- แก้ `DefaultYear_IsThaiYear` pre-existing failure (ปีไทย vs คริสต์ศักราชใน InvoiceViewModel constructor)
- Merge PR #30 (`frontend/abbr-invoice-running-number-ui`) ที่ค้างอยู่ก่อน
