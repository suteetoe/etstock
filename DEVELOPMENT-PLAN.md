# ETStock — ลำดับแผนการพัฒนา (Development Plan)

ระบบช่วยออกใบกำกับภาษีอย่างย่อสำหรับร้านค้าปลีก
Avalonia 11.3 (.NET 10) · MVVM (CommunityToolkit.Mvvm) · PostgreSQL

โครงสร้างทีม (goose multi-agent):

```
        Product Owner (คน)
                │  คำสั่ง/feature
                ▼
        Team Lead (parent recipe) ── วางแผน · แตกงาน · ตรวจรับ · รายงาน
                │ สั่งงานตามลำดับ dependency
     ┌──────────┼──────────┐
     ▼          ▼          ▼
  Backend    Frontend     QA      (sub-recipes / worker agents)
```

การส่งต่องานระหว่าง agent ใช้ไฟล์จริงในโฟลเดอร์ `<project>/.team/` เป็นสื่อกลาง
(เช่น `backend-contract-*.md`, `frontend-notes-*.md`, `qa-report-*.md`) เพราะ sub-recipe
แต่ละตัวรันแยก state กัน จึงต้องส่งบริบทผ่าน disk

---

## กลยุทธ์ Git / PR: 1 phase = 1 branch = 1 PR

แต่ละ phase ที่ทำเสร็จจะถูก "แตกออกเป็น PR" หนึ่งใบ เข้าสู่ branch ปลายทาง (`develop`/`main`)

```
develop ──┬─────────────────────────────────────► (merge PR ทีละ phase)
          │
          └─ feature/phase-2-product-stock
                 ├─ feat(be): ... [phase 2]      ◄── Backend commit
                 ├─ feat(fe): ... [phase 2]      ◄── Frontend commit
                 └─ test(qa): ... [phase 2]      ◄── QA commit
                        │
                        └─ QA verdict = PASS ──► Team Lead เปิด PR "[Phase 2] Product & Stock"
```

กติกา:
- **Team Lead** เป็นผู้สร้าง branch (`feature/phase-<n>-<slug>`), `git push` และ `gh pr create`
- **Backend / Frontend / QA** commit งานของตัวเองลง branch เดียวกัน (Conventional Commits:
  `feat(be):`, `feat(fe):`, `test(qa):`) แต่ **ไม่ push และไม่เปิด PR**
- เปิด PR **เฉพาะเมื่อ QA verdict = PASS** เท่านั้น; ถ้า FAIL จะวนแก้บน branch เดิมก่อน
- PR body สร้างจาก `<project>/.team/pr-body-<phase>.md` (สรุป BE/FE/QA + ผลทดสอบ + DoD checklist)
- ตั้ง draft PR ได้ด้วยพารามิเตอร์ `draft_pr=true`

> ต้องการ `gh` CLI ที่ `gh auth login` ไว้แล้ว (ผ่าน shell ของ developer extension)

---

## ลำดับ Phase

แต่ละ phase ทำเป็นรอบ (sprint) วนซ้ำ: Team Lead วางแผน → Backend → Frontend → QA → ตรวจรับ

### Phase 0 — Foundation
ตั้งฐานให้ทุกอย่างต่อยอดได้

| ทีม | งาน |
|-----|-----|
| Backend | scaffold solution (Domain/Data/App/Tests), ตั้งค่า Npgsql, ออกแบบ schema + migration เริ่มต้น (company, product, monthly_stock, abbr_invoice, abbr_invoice_item) |
| Frontend | scaffold ETStock.App (Avalonia + CommunityToolkit.Mvvm), MainWindow + navigation shell, theme/ภาษาไทย |
| QA | ตั้ง test project, smoke test ว่า build + รัน + เชื่อม DB ได้ |

**DoD:** `dotnet build` ผ่าน, แอปเปิดได้, migration รันกับ PostgreSQL ได้

---

### Phase 1 — Company Settings
ต้องมาก่อน เพราะ VAT rate / prefix เลขใบกำกับถูกใช้ใน phase หลัง

| ทีม | งาน |
|-----|-----|
| Backend | entity `Company` + repository (CRUD), validate เลขผู้เสียภาษี, reset_mode |
| Frontend | หน้ารายละเอียดบริษัท: ชื่อ, เลขผู้เสียภาษี, ที่อยู่, สาขา, prefix, อัตรา VAT |
| QA | test validate ข้อมูลบริษัท + persist/load ค่ากลับถูกต้อง |

**DoD:** บันทึก/โหลดข้อมูลบริษัทได้ครบ, VAT rate ใช้งานได้จริง

---

### Phase 2 — Product & Stock
สินค้าและสต๊อกยกมา/ต้นทุน + การคำนวณยกไป

| ทีม | งาน |
|-----|-----|
| Backend | entity `Product`, `MonthlyStock`, repository, logic `closing = opening + buy − sellFull − sellPos` |
| Frontend | หน้าเพิ่มสินค้า (รหัส/ชื่อ/หน่วย/ต้นทุน/ราคาขาย) + ตารางสินค้าในแท็บแรก (ช่อง "ยกไป" คำนวณสด) |
| QA | test สูตรยกไป (รวมเคสติดลบ/ศูนย์) + เพิ่มสินค้าซ้ำรหัส |

**DoD:** เพิ่มสินค้าได้, ตารางแสดงยกมา/ต้นทุน, ยกไปคำนวณถูก

---

### Phase 3 — Monthly Transaction
เลือกเดือน, กรอกซื้อ/ขาย/ขายหน้าร้าน, บันทึก, นำเข้า, เปลี่ยนเดือน

| ทีม | งาน |
|-----|-----|
| Backend | บันทึกธุรกรรมรายเดือนต่อสินค้า, logic ยกยอด `ยกไป(N) = ยกมา(N+1)`, import คงเหลือ |
| Frontend | ตัวเลือกเดือน/ปี, ปุ่มเปลี่ยนเดือน/นำเข้าคงเหลือ/บันทึก, ช่องกรอกซื้อ/ขายเต็ม/ขายหน้าร้าน |
| QA | test chain ยกยอดข้ามเดือน, บันทึกแล้วโหลดกลับ, import |

**DoD:** กรอก+บันทึกครบ, เปลี่ยนเดือนแล้วยกยอดต่อเนื่องถูกต้อง

---

### Phase 4 — Abbreviated Tax Invoice
หัวใจของระบบ: สร้างใบกำกับย่อจากยอดขายหน้าร้าน

| ทีม | งาน |
|-----|-----|
| Backend | สร้างใบกำกับย่อจาก sell_pos, running invoice_no (+prefix/reset), VAT-inclusive (net=amount/1.07), ลบ+สร้างใหม่แบบ atomic |
| Frontend | ปุ่ม "สร้างใบกำกับย่อ" + dialog ยืนยันเมื่อมีของเดือนนั้นแล้ว, แท็บรายการใบกำกับ (เลขที่/วันที่/รายการ/มูลค่า/ภาษี) |
| QA | test running number + reset, VAT/ปัดเศษ, flow เตือนสร้างซ้ำ + atomic |

**DoD:** สร้างใบกำกับย่อถูกต้องตามยอดขายหน้าร้าน, เตือนซ้ำทำงาน, VAT ถูก

---

### Phase 5 — Printing
พิมพ์ผ่าน Print Preview → เครื่องพิมพ์ระบบ

| ทีม | งาน |
|-----|-----|
| Backend | จัดรูปแบบข้อมูลใบกำกับสำหรับพิมพ์ (document model) |
| Frontend | Print Preview Dialog, เลือกเครื่องพิมพ์ของระบบ, เลย์เอาต์ใบกำกับย่อ |
| QA | test เนื้อหาเอกสารพิมพ์ครบ (เลขที่/บริษัท/รายการ/VAT), พรีวิวเปิดได้ |

**DoD:** พรีวิวแล้วสั่งพิมพ์ไปเครื่องพิมพ์ระบบได้

---

### Phase 6 — Hardening
รวมระบบ, E2E, ขัดเกลา

| ทีม | งาน |
|-----|-----|
| Backend | ทบทวน transaction/edge cases, performance query รายเดือน |
| Frontend | ขัดเกลา UX, error handling, ข้อความภาษาไทย, empty states |
| QA | integration/E2E ครบ flow (เลือกเดือน→กรอก→บันทึก→สร้างใบกำกับ→พิมพ์), regression |

**DoD:** E2E ผ่านทั้ง flow, ไม่มีบั๊ก severity สูงค้าง

---

## ตารางลำดับ & dependency

| Phase | ขึ้นกับ | ทำขนานได้ไหม |
|-------|---------|---------------|
| 0 Foundation | — | BE ก่อน แล้ว FE/QA |
| 1 Company | 0 | BE → FE → QA |
| 2 Product/Stock | 0 | BE → FE → QA |
| 3 Transaction | 2 | BE → FE → QA |
| 4 Invoice | 1, 3 | BE → FE → QA |
| 5 Printing | 4 | BE → FE → QA |
| 6 Hardening | 1-5 | ทำพร้อมกันได้ |

> กติกาภายในทุก phase: Backend ปิดสัญญา interface ก่อน Frontend ค่อยเริ่ม,
> QA ปิดท้ายและเป็นผู้ให้ verdict PASS/FAIL ก่อน Team Lead รายงาน PO
> และเมื่อ PASS แล้ว Team Lead จะเปิด **1 PR ต่อ phase** เข้า branch ปลายทางทันที
