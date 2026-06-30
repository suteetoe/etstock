# Backend Brief — Fix InvoiceDate timestamptz Kind error

## Context
- Agent: Backend sub-recipe agent
- Branch (prepared by Lead, อยู่บนนี้แล้ว ห้ามสร้าง/สลับ branch เอง): `backend/fix-invoicedate-timestamptz`
- Bug ที่ผู้ใช้รายงาน: กดปุ่ม "สร้างใบเสร็จอย่างย่อ" (Generate Abbr Invoices) แล้วเกิด exception ตอน `SaveChangesAsync`:
  ```
  System.ArgumentException: Cannot write DateTime with Kind=Unspecified to PostgreSQL type
  'timestamp with time zone', only UTC is supported.
  ```
- Root cause: `AbbrInvoice.InvoiceDate` ถูก map เป็น Postgres column type `timestamp with time zone`
  (ดู `ETStock/Data/Migrations/20260624035619_Initial.cs:22`) แต่ในทางธุรกิจ `InvoiceDate` คือ
  "วันที่ใบกำกับภาษีอย่างย่อ" เท่านั้น — ไม่มี time-of-day หรือ timezone ที่มีความหมายจริง
  และถูกสร้างด้วย `new DateTime(taxYear, taxMonth, day)` ซึ่งได้ `DateTimeKind.Unspecified`
  (ดู `ETStock/Services/InvoiceGeneratorService.cs:76`) — Npgsql เวอร์ชันปัจจุบันปฏิเสธ
  `Kind=Unspecified` สำหรับ `timestamptz` โดยเด็ดขาด

## Read these files first
- `ETStock/Models/AbbrInvoice.cs`
- `ETStock/Data/AppDbContext.cs`
- `ETStock/Data/Migrations/20260624035619_Initial.cs`
- `ETStock/Data/Migrations/AppDbContextModelSnapshot.cs`
- `ETStock/Services/InvoiceGeneratorService.cs` (อ่านเพื่อเข้าใจบริบท **ห้ามแก้ไฟล์นี้**)
- `ETStock/ViewModels/InvoiceViewModel.cs` (อ่านเพื่อเข้าใจบริบท **ห้ามแก้ไฟล์นี้**)

## Scope (แก้ได้เฉพาะไฟล์เหล่านี้เท่านั้น)
- `ETStock/Data/AppDbContext.cs`
- `ETStock/Data/Migrations/**` (เพิ่ม migration ใหม่เท่านั้น ห้ามแก้ไฟล์ migration เก่าที่ apply ไปแล้ว)
- `ETStock.Tests/**` (เพิ่ม/แก้ test ที่เกี่ยวข้องกับ fix นี้เท่านั้น)

**ห้ามแก้** `ETStock/Services/InvoiceGeneratorService.cs` หรือ ViewModel ใดๆ — fix นี้ต้องแก้ที่
domain/schema (column type) ไม่ใช่แก้ที่จุดสร้าง `DateTime`

## Task
แก้ root cause แบบ "ตรง domain": เปลี่ยน mapping ของ `AbbrInvoice.InvoiceDate` จาก Postgres type
`timestamp with time zone` เป็น `timestamp without time zone` เพราะ field นี้ไม่มีความหมายเรื่อง
timezone เลย (เป็นวันที่ใบกำกับ ไม่ใช่ event timestamp)

ขั้นตอน:
1. ใน `AppDbContext.OnModelCreating` เพิ่ม fluent config (ใช้ pattern เดียวกับ `HasPrecision` ที่มีอยู่แล้วในไฟล์นี้):
   ```csharp
   modelBuilder.Entity<AbbrInvoice>()
       .Property(i => i.InvoiceDate)
       .HasColumnType("timestamp without time zone");
   ```
2. สร้าง EF Core migration ใหม่ (ห้ามแก้ไฟล์ migration เก่า):
   ```
   dotnet ef migrations add ChangeInvoiceDateColumnType --project ETStock --startup-project ETStock
   ```
   ตรวจว่า migration ที่ออกมามี `AlterColumn<DateTime>("InvoiceDate", ... type: "timestamp without time zone" ...)`
   ในทั้ง `Up` และ `Down` (ฝั่ง Down ให้กลับเป็น `timestamp with time zone`)
3. ตรวจว่า `AppDbContextModelSnapshot.cs` ถูกอัปเดต sync กับ migration ใหม่อัตโนมัติ
4. รัน `dotnet build` ให้ผ่าน 0 error
5. รัน `dotnet test` ทั้งหมดให้ผ่าน — ห้ามมี regression
6. เพิ่ม unit test อย่างน้อย 1 เคสใน `ETStock.Tests` ยืนยันว่า entity `AbbrInvoice` ที่มี
   `InvoiceDate` เป็น `DateTimeKind.Unspecified` (เช่นจาก `new DateTime(y, m, d)`) สามารถ save
   ผ่าน repository ได้โดยไม่ throw (ใช้ EF InMemory provider ได้ตามแบบ test เดิมในโปรเจกต์)
   — comment สั้นๆ ในเทสว่า exception จริงเกิดกับ Npgsql provider เท่านั้น (InMemory ไม่บังคับ Kind)
   เทสนี้เป็น regression guard เชิง structure ของ entity/repository ไม่ใช่ทดสอบ Npgsql driver ตรงๆ

## Acceptance criteria
- `dotnet build` ผ่าน 0 error
- `dotnet test` ผ่านทั้งหมด ไม่มี regression
- มี migration ใหม่ที่ `AlterColumn` คอลัมน์ `InvoiceDate` เป็น `timestamp without time zone`
- ไม่มีการแก้ logic ใน `InvoiceGeneratorService.cs` หรือ ViewModel ใดๆ
- commit งานลง branch ปัจจุบัน `backend/fix-invoicedate-timestamptz` ด้วย Conventional Commits
  prefix `fix(be):` (และ/หรือ `chore(db):` สำหรับ migration) — **ห้าม push, ห้ามเปิด PR**
