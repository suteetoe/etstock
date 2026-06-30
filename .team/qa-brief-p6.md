# QA Brief — P6: InvoiceGeneratorService Tests

## Context
ทดสอบ `InvoiceGeneratorService` ที่ Backend สร้าง และทดสอบ `GenerateInvoicesCommand`
ใน `ProductViewModel` ว่าทำงานถูกต้อง

## Read First
- `ETStock/Services/IInvoiceGeneratorService.cs` — interface ที่จะทดสอบ
- `ETStock/Services/InvoiceGeneratorService.cs` — implementation
- `ETStock/Data/Repositories/IAbbrInvoiceRepository.cs` — รวม DeleteByPeriodAsync
- `ETStock/Data/Repositories/AbbrInvoiceRepository.cs`
- `ETStock/ViewModels/MonthlyStockViewModels.cs` — ProductViewModel.GenerateInvoicesCommand
- `ETStock.Tests/AbbrInvoiceRepositoryTests.cs` — ดูรูปแบบ test ที่มีอยู่

## Interface Spec (สำหรับ compile tests)

```csharp
// ETStock/Services/IInvoiceGeneratorService.cs
namespace ETStock.Services;

public record PosStockLine(string ProductName, decimal SellPosQty, decimal SellPrice);
public record GenerateInvoicesResult(int InvoiceCount, decimal TotalAmount, decimal VatAmount);

public interface IInvoiceGeneratorService
{
    Task<int> GetExistingCountAsync(int taxYear, int taxMonth, CancellationToken ct = default);
    Task<GenerateInvoicesResult?> GenerateAsync(
        int taxYear, int taxMonth,
        IReadOnlyList<PosStockLine> stockLines,
        bool replaceExisting = false,
        CancellationToken ct = default);
}
```

## Scope (แก้ได้เฉพาะไฟล์เหล่านี้)
- `ETStock.Tests/InvoiceGeneratorServiceTests.cs` — **ไฟล์ใหม่**
- `ETStock.Tests/AbbrInvoiceRepositoryDeleteByPeriodTests.cs` — **ไฟล์ใหม่**

## Task

### 1. `InvoiceGeneratorServiceTests.cs`

สร้าง helper เหมือนใน AbbrInvoiceRepositoryTests:
```csharp
private static AppDbContext CreateDb() => new(
    new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
```

**Test cases (อย่างน้อย 7 facts):**

#### TC-1: GenerateAsync_NoPosSales_ReturnsNull
- Input: stockLines ทุกตัวมี SellPosQty = 0
- Expected: result = null

#### TC-2: GenerateAsync_SingleProduct_CreatesInvoices
- Input: 1 product, SellPosQty = 5, SellPrice = 107m (VAT-inclusive)
- Expected:
  - result != null
  - InvoiceCount ≥ 1
  - sum ของ Items.Sum(i => i.Qty) ในทุกใบ = 5 (±0.02 rounding)
  - ทุกใบมี TaxYear, TaxMonth ถูกต้อง
  - ทุก item มี Amount ≈ round(qty * 107 / 1.07, 2)
  - ทุก item มี VatAmount ≈ qty * 107 - Amount

#### TC-3: GenerateAsync_MultipleProducts_AllQtyDistributed
- Input: 3 products แต่ละตัว SellPosQty = 3, SellPrice = 214m
- Expected:
  - sum qty ของแต่ละ product ใน invoices ทั้งหมด ≈ 3 (±0.02)
  - InvoiceCount ≥ 1

#### TC-4: GenerateAsync_SavesInvoicesToDb
- ใช้ InMemory DB (ผ่าน AppDbContext + AbbrInvoiceRepository)
- call GenerateAsync กับ 2 products
- assert: db.AbbrInvoices.Count() > 0
- assert: db.AbbrInvoiceItems.Count() > 0

#### TC-5: GenerateAsync_ReplaceExisting_DeletesOldInvoices
- seed 3 invoices เดิมใน DB
- call GenerateAsync ด้วย replaceExisting = true
- Expected: db.AbbrInvoices ต้องไม่มีใบเดิม (Id เดิมหายไป)
- Expected: มีใบใหม่ (ที่สร้างจาก GenerateAsync)

#### TC-6: GenerateAsync_ReplaceExisting_False_KeepsOldInvoices
- seed 1 invoice เดิม
- call GenerateAsync ด้วย replaceExisting = false (default)
- Expected: db.AbbrInvoices มีใบเดิม + ใบใหม่ รวมกัน > 1

#### TC-7: GetExistingCountAsync_ReturnsCorrectCount
- seed 2 invoices ใน period 2026/6, 1 invoice ใน period 2026/7
- call GetExistingCountAsync(2026, 6) → 2
- call GetExistingCountAsync(2026, 7) → 1
- call GetExistingCountAsync(2026, 8) → 0

### 2. `AbbrInvoiceRepositoryDeleteByPeriodTests.cs`

#### TC-8: DeleteByPeriodAsync_RemovesAllInvoicesInPeriod
- seed 3 invoices ใน 2026/6, 2 invoices ใน 2026/7
- call DeleteByPeriodAsync(2026, 6)
- Expected: db.AbbrInvoices.Count() = 2 (เหลือแค่ 2026/7)
- Expected: db.AbbrInvoiceItems ที่เกี่ยวกับ 2026/6 = 0

#### TC-9: DeleteByPeriodAsync_NothingToDelete_DoesNotThrow
- empty DB
- call DeleteByPeriodAsync(2026, 6) ต้องไม่ throw

## Acceptance Criteria
- [ ] `dotnet test` ผ่านทุก test (รวม test เดิมที่มีอยู่)
- [ ] test ทั้งหมดที่เขียนใหม่ผ่าน (≥ 9 facts)
- [ ] ครอบคลุม happy path, edge case (empty input, replace/keep existing)
- [ ] VAT calculation check ใน TC-2
