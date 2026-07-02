# QA Brief — Phase: pdf-preview-webview

## Context
- คุณคือ **QA Agent** ของทีม ETStock
- Branch ปัจจุบัน: `feature/phase-pdf-preview-webview`
- Working directory: `D:\Source\ETStock`
- Frontend ทำงานเสร็จแล้ว — เปลี่ยน `PdfPreviewWindow` จาก MuPDFCore ไปใช้ `Avalonia.WebView`

## เป้าหมาย
ตรวจสอบว่า phase นี้ build + test ผ่าน และ regression ไม่พัง

## Read these files first
1. `.team/plan-pdf-preview-webview.md`
2. `.team/frontend-brief-pdf-preview-webview.md`
3. `ETStock/Views/PdfPreviewWindow.axaml(.cs)` (หลัง FE แก้แล้ว)
4. `ETStock.Tests/` (ดู test เดิมทั้งหมด)

## Scope (ทำสิ่งนี้เท่านั้น)
- รัน `dotnet build` ที่ root solution
- รัน `dotnet test`
- ตรวจดูว่ามี test ที่ reference MuPDFCore หรือ PdfPreviewWindow หรือไม่ ถ้ามีให้ปรับให้คอมไพล์ผ่าน (ลบ using MuPDFCore หรือ mark เป็น skip พร้อมเหตุผล)
- **ห้ามแก้ production code** (Views, ViewModels, Services)

## ข้อควรตรวจ
1. Build ผ่าน ไม่มี error / warning ใหม่จากการลบ MuPDFCore
2. Test ที่มีอยู่ทั้งหมดผ่าน (xUnit)
3. ไม่มีไฟล์ test ที่ค้างอ้าง MuPDFCore แล้วคอมไพล์ไม่ผ่าน
4. ถ้าเขียน smoke test ได้ (เช่น ทดสอบว่า `PdfPreviewWindow` สร้างได้โดยไม่ throw) ให้เขียนเพิ่มใน `ETStock.Tests`

## Acceptance Criteria
- [ ] `dotnet build` ผ่าน
- [ ] `dotnet test` ผ่านทั้งหมด (x/y)
- [ ] ไม่มี test พังจากการเปลี่ยน package
- [ ] รายงาน verdict: PASS / FAIL + เหตุผล

## Commit
```
test(qa): verify pdf-preview-webview phase build + tests [phase pdf-preview-webview]
```
