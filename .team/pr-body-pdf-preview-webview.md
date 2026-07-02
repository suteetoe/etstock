# [Phase pdf-preview-webview] เปลี่ยน PDF Preview จาก MuPDF ไปใช้ Avalonia WebView

## สรุป Phase
เปลี่ยนหน้าจอ preview PDF ของใบกำกับภาษีอย่างย่อ จาก `PdfPreviewWindow` ที่ใช้ `MuPDFCore.MuPDFRenderer` (custom Avalonia control) ไปใช้ **`NativeWebView`** ของ Avalonia (`Avalonia.Controls.WebView` package) ฝังใน Window ของเราเอง พร้อม toolbar ปุ่ม "พิมพ์" และ "ปิด"

## รายการการเปลี่ยนแปลง

### Frontend (`feat(fe):`)
- **`ETStock/ETStock.csproj`** — ลบ `MuPDFCore.MuPDFRenderer 2.0.1`, เพิ่ม `Avalonia.Controls.WebView 11.4.0`
- **`ETStock/Views/PdfPreviewWindow.axaml`** — เปลี่ยน control จาก `PDFRenderer` เป็น `NativeWebView`, เอาปุ่ม ZoomIn/ZoomOut/FitPage ออก (WebView มี zoom ในตัว), เหลือ toolbar ปุ่ม "พิมพ์" + "ปิด"
- **`ETStock/Views/PdfPreviewWindow.axaml.cs`** — เขียนใหม่:
  - รับ `byte[]` PDF ใน constructor → เขียนลง temp file (`Path.GetTempPath()/etstock_invoice_{guid}.pdf`)
  - `Opened` → ตั้ง `WebView.Source` เป็น URI ของ temp file
  - `Closed` → ลบ temp file (best-effort)
  - `Print_Click` → เรียก `WebView.ShowPrintUI()`

### Backend
ไม่มีการเปลี่ยนแปลง — ใช้ `IInvoicePrintService.GeneratePdfAsync` ที่มีอยู่แล้ว

### QA (`test(qa):`)
- เพิ่ม `ETStock.Tests/PdfPreviewWindowSmokeTests.cs` — 5 smoke tests:
  - `PdfPreviewWindow_InheritsFromAvaloniaWindow`
  - `PdfPreviewWindow_HasParameterlessConstructor`
  - `PdfPreviewWindow_HasByteArrayConstructor`
  - `PdfPreviewWindow_DoesNotReferenceMuPDF`
  - `PdfPreviewWindow_AssemblyReferencesAvaloniaWebView`

## Business Rules ที่ครอบคลุม
Phase นี้เป็นการเปลี่ยน **UI rendering layer** ของ PDF preview เท่านั้น ไม่กระทบ business rules (VAT, running number, คงเหลือยกไป) — logic การสร้าง PDF ใน `InvoicePdfDocument` / `InvoicePrintService` เหมือนเดิมทุกประการ

## ผลการทดสอบ
- `dotnet build`: **PASS** (0 errors, 8 warnings pre-existing NU1903)
- `dotnet test`: **88 passed / 1 skipped / 2 failed**
  - 1 skip: `CanBeInstantiatedWithPdfBytes` — `NativeWebView` ต้องการ native display context รัน headless ไม่ได้ (expected)
  - 2 fail: pre-existing bugs ไม่เกี่ยวกับ phase นี้ (`InvoiceViewModelTests.DefaultYear_IsThaiYear`, `MonthlyStockRepositoryTests.CarryForwardAsync_UpdatesOpeningWithoutReplacingCurrentTransactions`)
- **QA verdict: PASS** ✓

## Checklist Definition of Done
- [x] มี brief ครบทุก task (FE, QA)
- [x] อยู่บน feature branch `feature/phase-pdf-preview-webview`
- [x] `dotnet build` ผ่าน ไม่มี error
- [x] UI ผูกกับ ViewModel ตาม MVVM (ไม่มี logic ใหม่ใน code-behind นอกจาก lifecycle ของ WebView + temp file)
- [x] MuPDFCore ถูกลบออกสมบูรณ์
- [x] temp file ถูกลบเมื่อปิด dialog
- [x] QA verdict = PASS
- [x] เปิด PR เข้า `develop`

## ความเสี่ยง
- `Avalonia.Controls.WebView 11.4.0` เป็น package กึ่งทางการ — ใช้ native WebView ของ OS (Edge WebView2 บน Windows) จึงต้องติดตั้ง WebView2 Runtime บนเครื่องผู้ใช้ (ติดตั้ง default บน Windows 10/11 เวอร์ชันล่าสุด)
- Smoke test 1 ตัวถูก skip เพราะ NativeWebView ต้องการ display context — การทดสอบ UI จริงต้องรัน manual บนเครื่องที่มีจอ

## Phase ถัดไปที่แนะนำ
1. แก้ 2 pre-existing bug ที่ QA พบ (Thai year ใน InvoiceViewModel, CarryForward logic ใน MonthlyStockRepository)
2. ถอด `PrintPreviewWindow` / `PrintPreviewViewModel` เก่าออก ถ้าไม่ใช้แล้ว (มีงานซ้อนทับกับ PdfPreviewWindow)
3. ปรับ InvoiceView ให้ดึงปี/เดือนจาก PeriodSelector อัตโนมัติ
