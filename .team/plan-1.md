# Phase 1 — Company Details Setup

**Branch:** `feature/phase-1-company`
**Base:** `develop`
**Date:** 2026-06-24

---

## เป้าหมาย

ใช้งานหน้า "ข้อมูลบริษัท" ให้ครบ — ฟอร์มกรอก/บันทึกข้อมูลบริษัท (ชื่อ, เลขผู้เสียภาษี, ที่อยู่, สาขา, prefix ใบกำกับ, อัตรา VAT) พร้อม persistence ลง PostgreSQL

---

## สถานะ Phase 0 ที่ handoff มา

- Entity `Company` พร้อมแล้ว (`ETStock/Models/Company.cs`)
- `AppDbContext` มี `DbSet<Company> Companies` และ migration แล้ว
- `CompanyViewModel` และ `CompanyView` เป็นแค่ stub placeholder
- DI host ใน `Program.cs` พร้อม; `Program.Services` เป็น `public static IServiceProvider?`

---

## Tasks และ Dependency

```
Backend (codex)  →  Frontend (antigravity/codex)  →  QA (gemini)
```

### Task B1 — Backend: ICompanyRepository + CompanyRepository

**Scope:** `ETStock/Data/Repositories/`

ให้สร้าง:
- `ICompanyRepository.cs` (interface)
  - `Task<Company?> GetAsync(CancellationToken ct = default)`
  - `Task UpsertAsync(Company company, CancellationToken ct = default)`
- `CompanyRepository.cs` (implements ICompanyRepository with EF Core async)
  - GetAsync: ดึง record แรกจาก Companies (บริษัทมีแค่ 1 record)
  - UpsertAsync: ถ้ายังไม่มี record → Add; ถ้ามีแล้ว → Update แล้ว SaveChangesAsync
- ลงทะเบียนใน `Program.cs`:
  ```csharp
  services.AddScoped<ICompanyRepository, CompanyRepository>();
  ```
- unit tests ใน `ETStock.Tests/CompanyRepositoryTests.cs` (InMemory DB)

**Commits:** `feat(be): add ICompanyRepository + CompanyRepository [phase 1]`, `chore(be): register CompanyRepository in DI [phase 1]`

**Output:** `.team/backend-contract-1.md` (interface signature + DI usage example)

---

### Task F1 — Frontend: CompanyViewModel + CompanyView

**Scope:** `ETStock/ViewModels/CompanyViewModel.cs`, `ETStock/Views/CompanyView.axaml`

ให้แก้:
- `CompanyViewModel.cs`:
  - Design-time constructor (parameterless) — ไม่ inject dependency
  - Runtime constructor `(ICompanyRepository repository)` — ใช้จริง
  - `[ObservableProperty]` สำหรับ: `Name`, `TaxId`, `Address`, `BranchName`, `BranchCode`, `InvoicePrefix`, `VatRate` (decimal → string สำหรับ binding)
  - `[ObservableProperty] bool IsBusy`
  - `[RelayCommand] async Task LoadAsync()` — เรียก repository.GetAsync() แล้ว map ลง properties
  - `[RelayCommand] async Task SaveAsync()` — สร้าง Company object จาก properties แล้ว repository.UpsertAsync()
- `MainWindowViewModel.cs`:
  - ดึง `ICompanyRepository` จาก `Program.Services` แล้วส่งเข้า CompanyViewModel constructor
- `CompanyView.axaml`:
  - ลบ placeholder ออก
  - ใส่ form layout: Grid 2 คอลัมน์ (Label, TextBox) สำหรับแต่ละ field
  - บรรทัดสุดท้ายมีปุ่ม "บันทึก" bound กับ `SaveCommand`
  - ระหว่าง busy ให้ปุ่มเป็น disabled

**Commits:** `feat(fe): implement CompanyViewModel + CompanyView form [phase 1]`

**Output:** `.team/frontend-notes-1.md`

---

### Task Q1 — QA: Tests + Verdict

**Scope:** `ETStock.Tests/`

ให้เขียน:
- `ETStock.Tests/CompanyRepositoryTests.cs` (xUnit + InMemory DB):
  - `GetAsync_ReturnsNull_WhenEmpty`
  - `UpsertAsync_Inserts_WhenNoRecord`
  - `UpsertAsync_Updates_WhenRecordExists`
- `ETStock.Tests/CompanyViewModelTests.cs` (xUnit):
  - `LoadAsync_PopulatesProperties_FromRepository`
  - `SaveAsync_CallsUpsert_WithCorrectValues`
- รัน `dotnet test ETStock.slnx` และรายงานผล

**ห้ามแก้ production code** — ถ้า build หรือ test ไม่ผ่าน ให้รายงาน FAIL พร้อมระบุจุดที่พัง

**Output:** `.team/qa-report-1.md` พร้อม verdict PASS/FAIL

---

## Definition of Done

- [ ] Build ผ่าน (`dotnet build ETStock.slnx`) — 0 errors
- [ ] Tests ผ่านทั้งหมด (`dotnet test ETStock.slnx`)
- [ ] ICompanyRepository + CompanyRepository ครบ
- [ ] CompanyView ฟอร์มแสดงและบันทึกข้อมูลได้ทุก field
- [ ] CompanyViewModel ผูกกับ interface ของ backend ผ่าน constructor injection
- [ ] ไม่มี logic ใน code-behind (.axaml.cs)
- [ ] QA verdict = PASS
- [ ] PR เปิดเข้า `develop` เรียบร้อย
