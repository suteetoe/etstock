# Backend Brief — P6: InvoiceGeneratorService

## Context
ต้องการ service ที่รับยอด SellPosQty ของแต่ละสินค้าในเดือนนั้น แล้วแจก qty แบบสุ่มลงในหลายใบกำกับภาษีอย่างย่อ (AbbrInvoice) ให้ครบทุกรายการ

SellPosQty = จำนวนสินค้าที่ขายหน้าร้าน (POS) ซึ่งยังไม่ได้ออกใบกำกับ
SellPrice ในฐานข้อมูล = ราคา **รวม VAT 7%** (VAT-inclusive)

## Read First
- `ETStock/Data/Repositories/IAbbrInvoiceRepository.cs`
- `ETStock/Data/Repositories/AbbrInvoiceRepository.cs`
- `ETStock/Models/AbbrInvoice.cs`
- `ETStock/Models/AbbrInvoiceItem.cs`
- `ETStock/Services/IInvoicePrintService.cs` (ดูรูปแบบ interface)
- `ETStock/Program.cs` (ดูวิธีลง DI)

## Scope (แก้ได้เฉพาะไฟล์เหล่านี้)
- `ETStock/Data/Repositories/IAbbrInvoiceRepository.cs` — เพิ่ม `DeleteByPeriodAsync`
- `ETStock/Data/Repositories/AbbrInvoiceRepository.cs` — implement `DeleteByPeriodAsync`
- `ETStock/Services/IInvoiceGeneratorService.cs` — **ไฟล์ใหม่**
- `ETStock/Services/InvoiceGeneratorService.cs` — **ไฟล์ใหม่**
- `ETStock/Program.cs` — ลง DI

## Task

### 1. เพิ่ม `DeleteByPeriodAsync` ใน `IAbbrInvoiceRepository`
```csharp
Task DeleteByPeriodAsync(int taxYear, int taxMonth, CancellationToken ct = default);
```
Implementation ใน `AbbrInvoiceRepository`:
- ดึง AbbrInvoice ทั้งหมดของ (taxYear, taxMonth)
- ลบแบบ cascade (Items ลบตาม EF relationship)
- `SaveChangesAsync()` ครั้งเดียว

### 2. สร้าง `IInvoiceGeneratorService.cs`

```csharp
namespace ETStock.Services;

public record PosStockLine(string ProductName, decimal SellPosQty, decimal SellPrice);
public record GenerateInvoicesResult(int InvoiceCount, decimal TotalAmount, decimal VatAmount);

public interface IInvoiceGeneratorService
{
    /// จำนวนใบกำกับที่มีอยู่ในงวดนั้น (สำหรับ UI ถาม confirm)
    Task<int> GetExistingCountAsync(int taxYear, int taxMonth, CancellationToken ct = default);

    /// สร้างใบกำกับสุ่มจาก POS qty
    /// - ถ้า replaceExisting = true ให้ลบใบเดิมทั้งหมดก่อน
    /// - คืน null ถ้าไม่มี stockLine ใดที่ SellPosQty > 0
    Task<GenerateInvoicesResult?> GenerateAsync(
        int taxYear,
        int taxMonth,
        IReadOnlyList<PosStockLine> stockLines,
        bool replaceExisting = false,
        CancellationToken ct = default);
}
```

### 3. สร้าง `InvoiceGeneratorService.cs`

**Constructor:** inject `IAbbrInvoiceRepository`

**`GetExistingCountAsync`:**
```csharp
var list = await _repo.GetByPeriodAsync(taxYear, taxMonth);
return list.Count;
```

**`GenerateAsync` — algorithm:**

```
1. filter stockLines ที่ SellPosQty > 0  → validLines
2. ถ้า validLines ว่าง → return null
3. totalQty = sum(validLines.SellPosQty)
4. maxInvoices = min(20, max(1, (int)Math.Ceiling(totalQty)))
   N = rng.Next(1, maxInvoices + 1)         // N ∈ [1, maxInvoices]
5. slots = new decimal[N][][] สำหรับเก็บ qty ของแต่ละ (invoice, product)
   สำหรับ product แต่ละตัว:
       parts = RandomPartition(line.SellPosQty, N, rng)
       บันทึก parts[i] ลง slot[i][product]
6. สร้าง List<AbbrInvoice>:
   สำหรับ i = 0..N-1:
       items = รวมทุก product ที่ parts[i] > 0
       ถ้า items ว่าง → ข้าม (empty slot)
       วันที่ = DateTime ของ taxYear/taxMonth วัน random ใน [1, daysInMonth]
       InvoiceNo = $"ABB-{taxYear}{taxMonth:00}-{seq:000}"  (seq = 1-based ในรอบนี้)
       สำหรับแต่ละ item:
           amount = round(qty * sellPrice / 1.07m, 2)
           vatAmount = round(qty * sellPrice - amount, 2)
       invoice.TotalAmount = sum(amount + vatAmount)
       invoice.VatAmount   = sum(vatAmount)
       TaxYear = taxYear, TaxMonth = taxMonth
7. ถ้า replaceExisting → await _repo.DeleteByPeriodAsync(taxYear, taxMonth)
8. บันทึกทุกใบ: foreach invoice → await _repo.SaveAsync(invoice)
9. return GenerateInvoicesResult(invoiceCount, totalAmount, totalVat)
```

**`RandomPartition` helper (private static):**
```csharp
private static decimal[] RandomPartition(decimal total, int n, Random rng)
{
    if (n == 1) return [total];
    var cuts = new decimal[n - 1];
    for (int i = 0; i < n - 1; i++)
        cuts[i] = Math.Round((decimal)(rng.NextDouble() * (double)total), 2);
    Array.Sort(cuts);
    var result = new decimal[n];
    decimal prev = 0;
    for (int i = 0; i < n - 1; i++)
    {
        result[i] = cuts[i] - prev;
        prev = cuts[i];
    }
    result[n - 1] = Math.Round(total - prev, 2);
    return result;
}
```

หมายเหตุ: บางช่องอาจได้ 0 → ถือว่า empty ให้ข้ามไม่สร้าง item นั้น

### 4. ลง DI ใน `Program.cs`
```csharp
services.AddScoped<IInvoiceGeneratorService, InvoiceGeneratorService>();
```
เพิ่มหลัง `services.AddScoped<IInvoicePrintService, InvoicePrintService>();`

### 5. เขียน Backend Contract
เขียนไฟล์ `.team/backend-contract-p6.md` ระบุ interface, records, และตัวอย่างการเรียกใช้ให้ Frontend นำไปใช้

## Acceptance Criteria
- [ ] `dotnet build` ผ่าน 0 errors
- [ ] `DeleteByPeriodAsync` ลบ AbbrInvoice + Items ของงวดนั้นทั้งหมด
- [ ] `GetExistingCountAsync` คืนจำนวนที่ถูกต้อง
- [ ] `GenerateAsync` คืน null เมื่อไม่มี POS qty
- [ ] sum ของ qty แต่ละ product ใน invoices ที่สร้าง = SellPosQty เดิม (±rounding)
- [ ] VAT calculation ถูก: Amount = round(qty*price/1.07, 2), VatAmount = qty*price - Amount
- [ ] รัน `dotnet test` แล้วไม่มี test เดิมพัง
