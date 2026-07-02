# Phase: pdf-preview-webview

## เป้าหมาย
เปลี่ยนหน้าจอ preview PDF ของใบกำกับภาษีอย่างย่อ จาก `PdfPreviewWindow` (MuPDFCore custom control) ไปใช้ **WebView** ของ Avalonia ฝังใน Window ของเราเอง พร้อม toolbar ปุ่มพิมพ์ + ปิด

## ที่มา / ความต้องการจาก PO
- ใช้ WebView ของ Avalonia (เลือก `Avalonia.WebView` package) แทน MuPDF
- Dialog เป็น Window ของเราเอง + toolbar (พิมพ์, ปิด)
- สั่งพิมพ์ผ่านปุ่มของเรา → เรียกใช้ฟังก์ชันพิมพ์ของ WebView
- PDF เก็บใน temp file, ลบตอนปิด dialog

## การเปลี่ยนแปลงหลัก
1. เพิ่ม NuGet: `Avalonia.WebView` (และ dependencies ที่จำเป็น)
2. ลบ `MuPDFCore.MuPDFRenderer` package
3. แก้ `Views/PdfPreviewWindow.axaml(.cs)` — ใช้ `WebView` แทน `PDFRenderer`
4. `InvoiceView.OnPdfPreviewRequested` — ปรับเล็กน้อย (ยังเปิด `PdfPreviewWindow` เหมือนเดิม แค่ส่ง byte[])
5. (อาจ) เปลี่ยน signature รับ `byte[]` → เขียน temp file ภายใน Window เอง

## งานแยกตามบทบาท
- **Backend**: ไม่มีงาน DB/entity — interface `IInvoicePrintService.GeneratePdfAsync(byte[])` มีอยู่แล้ว ใช้ได้เลย
- **Frontend** (งานหลัก): เพิ่ม package, แก้ PdfPreviewWindow, จัดการ temp file lifecycle
- **QA**: เขียน/ปรับ test ที่เกี่ยวกับ PdfPreviewWindow ถ้ามี และรายงานผล build + smoke test

## Definition of Done
- `dotnet build` ผ่าน ไม่มี error
- `dotnet test` ผ่านทั้งหมด
- `PdfPreviewWindow` ใช้ WebView ของ Avalonia ไม่พึ่ง MuPDFCore อีก
- temp file ถูกลบเมื่อปิด dialog
- ปุ่มพิมพ์ + ปิด ทำงานได้
- เปิด PR เข้า `develop`

## ลำดับ dependency
FE ทำพร้อมกันได้เลย (ไม่ต้องรอ BE) → QA รอ FE เสร็จ
