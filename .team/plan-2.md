# Phase 2 — Product CRUD

**Branch pattern:** `backend/phase2-product-repo`, `frontend/phase2-product-ui`, `qa/phase2-product-tests`
**Base:** `develop`
**Date:** 2026-06-24

---

## เป้าหมาย

ใช้งานหน้า "สินค้า / สต๊อก" ให้เป็น CRUD สมบูรณ์ — ตารางรายการสินค้า, ฟอร์มเพิ่ม/แก้ไข/ลบสินค้า
(รหัส, ชื่อ, หน่วย, ต้นทุน, ราคาขาย) พร้อม persistence ลง PostgreSQL

---

## สถานะ Phase 1 ที่ handoff มา

- Entity `Product` พร้อมแล้ว (`ETStock/Models/Product.cs`)
  - Id, Code (unique), Name, Unit, CostPrice, SellPrice, MonthlyStocks (nav)
- `AppDbContext` มี `DbSet<Product> Products` + index unique ที่ Code
- `ProductViewModel` เป็น stub (แค่ Title property)
- `ProductView.axaml` เป็น placeholder StackPanel
- DI host ใน `Program.cs` พร้อม; มีแค่ CompanyRepository ลงทะเบียนอยู่

---

## Tasks และ Dependency

```
Backend (codex) → Frontend (antigravity) → QA (gemini)
```

### Task B2 — Backend: IProductRepository + ProductRepository

**Branch:** `backend/phase2-product-repo`
**Scope:** `ETStock/Data/Repositories/`, `ETStock/Program.cs`

สร้าง:
- `IProductRepository.cs`
  ```csharp
  Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
  Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
  Task<Product> AddAsync(Product product, CancellationToken ct = default);
  Task UpdateAsync(Product product, CancellationToken ct = default);
  Task DeleteAsync(int id, CancellationToken ct = default);
  ```
- `ProductRepository.cs` (EF Core async, ไม่ใช้ `.Result` / `.Wait()`)
  - GetAllAsync: `_db.Products.OrderBy(p => p.Code).ToListAsync(ct)` แล้ว cast เป็น `IReadOnlyList`
  - GetByIdAsync: `FirstOrDefaultAsync(p => p.Id == id, ct)`
  - AddAsync: `_db.Products.Add(product); await _db.SaveChangesAsync(ct); return product;`
  - UpdateAsync: ดึง existing → อัปเดตทุก field → SaveChangesAsync (throw KeyNotFoundException ถ้าไม่เจอ)
  - DeleteAsync: ดึง existing → Remove → SaveChangesAsync (throw KeyNotFoundException ถ้าไม่เจอ)
- ลงทะเบียนใน `Program.cs`:
  ```csharp
  services.AddScoped<IProductRepository, ProductRepository>();
  ```
- เขียน `ETStock.Tests/ProductRepositoryTests.cs` (xUnit + InMemory):
  - `GetAllAsync_ReturnsEmpty_WhenNoProducts`
  - `AddAsync_AddsProduct_AndReturnsWithId`
  - `GetByIdAsync_ReturnsProduct_WhenExists`
  - `UpdateAsync_UpdatesAllFields`
  - `DeleteAsync_RemovesProduct`

**Commit:** `feat(be): add IProductRepository + ProductRepository [phase 2]`
**Output:** `.team/backend-brief-phase2.md` (interface signature สำหรับให้ frontend อ้างอิง)

---

### Task F2 — Frontend: ProductViewModel + ProductView

**Branch:** `frontend/phase2-product-ui`
**Base:** merge จาก `backend/phase2-product-repo` ก่อน (เพื่อให้ interface พร้อม)
**Scope:** `ETStock/ViewModels/ProductViewModel.cs`, `ETStock/Views/ProductView.axaml`, `ETStock/ViewModels/MainWindowViewModel.cs`, `ETStock/Program.cs`

แก้ `ProductViewModel.cs`:
- Design-time constructor (parameterless)
- Runtime constructor `(IProductRepository repository)`
- `[ObservableProperty] ObservableCollection<ProductRow> Products` — ProductRow เป็น inner record/class:
  ```csharp
  public record ProductRow(int Id, string Code, string Name, string Unit, decimal CostPrice, decimal SellPrice);
  ```
- `[ObservableProperty] ProductRow? SelectedProduct`
- Form fields (string สำหรับ binding): `EditCode`, `EditName`, `EditUnit`, `EditCostPrice`, `EditSellPrice`
- `[ObservableProperty] bool IsFormVisible`
- `[ObservableProperty] bool IsBusy`
- `[ObservableProperty] string StatusMessage`
- `[ObservableProperty] bool IsEditMode` (true=แก้ไข existing, false=เพิ่มใหม่)
- Commands:
  - `[RelayCommand] async Task LoadAsync()` — GetAllAsync → map → Products
  - `[RelayCommand] void ShowAddForm()` — เคลียร์ฟอร์ม, IsFormVisible=true, IsEditMode=false
  - `[RelayCommand] void ShowEditForm()` — โหลด SelectedProduct ลงฟอร์ม, IsFormVisible=true, IsEditMode=true (CanExecute: SelectedProduct != null)
  - `[RelayCommand] async Task SaveAsync()` — ถ้า IsEditMode→UpdateAsync, ไม่งั้น→AddAsync, แล้ว LoadAsync, IsFormVisible=false
  - `[RelayCommand] async Task DeleteAsync()` — DeleteAsync(SelectedProduct.Id), LoadAsync (CanExecute: SelectedProduct != null)
  - `[RelayCommand] void CancelForm()` — IsFormVisible=false

แก้ `MainWindowViewModel.cs`:
- ดึง `IProductRepository` จาก `Program.Services` แล้วส่ง constructor injection เข้า ProductViewModel

แก้ `ProductView.axaml`:
- ลบ placeholder ออก
- Layout แนวตั้ง: ส่วนบน = ToolBar (ปุ่มเพิ่ม/แก้ไข/ลบ), กลาง = DataGrid, ล่าง = Form panel (visible เมื่อ IsFormVisible=true)
- DataGrid แสดง: Code, Name, Unit, CostPrice, SellPrice; bind `SelectedItem` → `SelectedProduct`
- Form panel (Grid 2 col): label + TextBox สำหรับแต่ละ Edit field, ปุ่ม "บันทึก" + "ยกเลิก"
- ปุ่มแก้ไขและลบ: `IsEnabled="{Binding SelectedProduct, Converter={x:Static ObjectConverters.IsNotNull}}"`

**Commit:** `feat(fe): implement ProductViewModel + ProductView CRUD [phase 2]`

---

### Task Q2 — QA: Tests + Verdict

**Branch:** `qa/phase2-product-tests`
**Base:** merge จาก `frontend/phase2-product-ui`
**Scope:** `ETStock.Tests/`

เขียน `ETStock.Tests/ProductViewModelTests.cs` (xUnit + mock/stub IProductRepository):
- `LoadAsync_PopulatesProducts_FromRepository`
- `ShowAddForm_ClearsForm_AndShowsForm`
- `ShowEditForm_PopulatesForm_FromSelectedProduct`
- `SaveAsync_CallsAddAsync_WhenNotEditMode`
- `SaveAsync_CallsUpdateAsync_WhenEditMode`
- `DeleteAsync_CallsDeleteAsync_WithSelectedId`
- `CancelForm_HidesForm`

รัน `dotnet test ETStock.slnx` — รายงานผลใน `.team/qa-report-2.md` พร้อม verdict PASS/FAIL

**ห้ามแก้ production code** — ถ้า build หรือ test ไม่ผ่านให้รายงาน FAIL พร้อมระบุจุด

---

## Definition of Done

- [ ] Build ผ่าน (`dotnet build ETStock.slnx`) — 0 errors
- [ ] Tests ผ่านทั้งหมด (`dotnet test ETStock.slnx`)
- [ ] IProductRepository + ProductRepository ครบ (GetAll, GetById, Add, Update, Delete)
- [ ] ProductView แสดงตารางสินค้า + ฟอร์มเพิ่ม/แก้ไข/ลบ
- [ ] ProductViewModel ผูกกับ IProductRepository ผ่าน constructor injection
- [ ] ไม่มี logic ใน code-behind (.axaml.cs)
- [ ] QA verdict = PASS
- [ ] PRs ทั้ง 3 ใบเปิดเข้า `develop` เรียบร้อย
