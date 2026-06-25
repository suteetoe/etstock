# Backend Brief — Phase 0 Foundation

**Branch:** feature/phase-0-foundation
**Commit convention:** `feat(be): <description> [phase 0]`
**Do NOT run git commands — Orchestrator handles all git.**

## Objective

Scaffold the data layer for ETStock (ระบบออกใบกำกับภาษีอย่างย่อ).  
Establish EF Core + Npgsql + entity models + DbContext + DI wiring + test project scaffold.

## Working directory: D:\Source\ETStock

## Tasks

### 1. Update ETStock/ETStock.csproj — add packages

Add these PackageReferences:
- `Microsoft.EntityFrameworkCore` Version="10.0.7"
- `Microsoft.EntityFrameworkCore.Design` Version="10.0.7" (with PrivateAssets="All")
- `Npgsql.EntityFrameworkCore.PostgreSQL` Version="10.0.0"
- `Microsoft.Extensions.Hosting` Version="10.0.0"

### 2. Create entity models in ETStock/Models/

**Company.cs**
```csharp
namespace ETStock.Models;
public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;           // ชื่อบริษัท
    public string TaxId { get; set; } = string.Empty;          // เลขผู้เสียภาษี 13 หลัก
    public string Address { get; set; } = string.Empty;        // ที่อยู่
    public string BranchName { get; set; } = string.Empty;     // ชื่อสาขา
    public string BranchCode { get; set; } = "00000";          // รหัสสาขา
    public string InvoicePrefix { get; set; } = string.Empty;  // prefix เลขใบกำกับ
    public decimal VatRate { get; set; } = 0.07m;              // อัตราภาษี (7%)
}
```

**Product.cs**
```csharp
namespace ETStock.Models;
public class Product
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;   // รหัสสินค้า (unique)
    public string Name { get; set; } = string.Empty;   // ชื่อสินค้า
    public string Unit { get; set; } = string.Empty;   // หน่วย เช่น ชิ้น, กล่อง
    public decimal CostPrice { get; set; }             // ต้นทุน
    public decimal SellPrice { get; set; }             // ราคาขาย
    public ICollection<MonthlyStock> MonthlyStocks { get; set; } = [];
}
```

**MonthlyStock.cs**
```csharp
namespace ETStock.Models;
public class MonthlyStock
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }          // 1-12
    public decimal OpeningQty { get; set; } // ยกมา
    public decimal BuyQty { get; set; }     // ซื้อ
    public decimal SellFullQty { get; set; }// ขายเต็มใบ
    public decimal SellPosQty { get; set; } // ขายหน้าร้าน
    // closing = opening + buy - sellFull - sellPos (computed by app, not DB)
}
```

**AbbrInvoice.cs**
```csharp
namespace ETStock.Models;
public class AbbrInvoice
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty; // เลขที่ใบกำกับ
    public DateTime InvoiceDate { get; set; }
    public int TaxYear { get; set; }    // ปีภาษี (พ.ศ.)
    public int TaxMonth { get; set; }   // เดือนภาษี 1-12
    public decimal TotalAmount { get; set; } // มูลค่ารวม (รวม VAT)
    public decimal VatAmount { get; set; }   // ภาษีมูลค่าเพิ่ม
    public ICollection<AbbrInvoiceItem> Items { get; set; } = [];
}
```

**AbbrInvoiceItem.cs**
```csharp
namespace ETStock.Models;
public class AbbrInvoiceItem
{
    public int Id { get; set; }
    public int AbbrInvoiceId { get; set; }
    public AbbrInvoice AbbrInvoice { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Qty { get; set; }
    public decimal Amount { get; set; }    // ก่อน VAT
    public decimal VatAmount { get; set; } // VAT ส่วนของรายการนี้
}
```

### 3. Create ETStock/Data/AppDbContext.cs

```csharp
using ETStock.Models;
using Microsoft.EntityFrameworkCore;

namespace ETStock.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<MonthlyStock> MonthlyStocks => Set<MonthlyStock>();
    public DbSet<AbbrInvoice> AbbrInvoices => Set<AbbrInvoice>();
    public DbSet<AbbrInvoiceItem> AbbrInvoiceItems => Set<AbbrInvoiceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Code).IsUnique();

        modelBuilder.Entity<MonthlyStock>()
            .HasIndex(ms => new { ms.ProductId, ms.Year, ms.Month }).IsUnique();

        modelBuilder.Entity<AbbrInvoice>()
            .Property(i => i.TotalAmount).HasPrecision(18, 4);
        modelBuilder.Entity<AbbrInvoice>()
            .Property(i => i.VatAmount).HasPrecision(18, 4);

        modelBuilder.Entity<AbbrInvoiceItem>()
            .Property(i => i.Amount).HasPrecision(18, 4);
        modelBuilder.Entity<AbbrInvoiceItem>()
            .Property(i => i.VatAmount).HasPrecision(18, 4);

        modelBuilder.Entity<Product>()
            .Property(p => p.CostPrice).HasPrecision(18, 4);
        modelBuilder.Entity<Product>()
            .Property(p => p.SellPrice).HasPrecision(18, 4);

        modelBuilder.Entity<MonthlyStock>()
            .Property(ms => ms.OpeningQty).HasPrecision(18, 4);
        modelBuilder.Entity<MonthlyStock>()
            .Property(ms => ms.BuyQty).HasPrecision(18, 4);
        modelBuilder.Entity<MonthlyStock>()
            .Property(ms => ms.SellFullQty).HasPrecision(18, 4);
        modelBuilder.Entity<MonthlyStock>()
            .Property(ms => ms.SellPosQty).HasPrecision(18, 4);
    }
}
```

### 4. Create ETStock/appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=etstock;Username=etstock;Password=etstock"
  }
}
```

Also ensure this file gets copied to output — update ETStock.csproj to add:
```xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### 5. Update ETStock/Program.cs — add DI + DbContext

Replace Program.cs with a version that:
- Builds a `Host` using `Microsoft.Extensions.Hosting`
- Registers `AppDbContext` with Npgsql using connection string from appsettings.json
- Stores the service provider as a static property for access from Avalonia app

```csharp
using Avalonia;
using ETStock.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace ETStock;

internal sealed class Program
{
    public static IServiceProvider? Services { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(AppContext.BaseDirectory)
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(ctx.Configuration.GetConnectionString("DefaultConnection")));
            })
            .Build();

        Services = host.Services;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

### 6. Scaffold ETStock.Tests project

Create `ETStock.Tests/ETStock.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ETStock\ETStock.csproj" />
  </ItemGroup>
</Project>
```

### 7. Update ETStock.slnx

Add the test project:
```xml
<Solution>
  <Project Path="ETStock/ETStock.csproj" />
  <Project Path="ETStock.Tests/ETStock.Tests.csproj" />
</Solution>
```

## Definition of Done (for this agent's scope)
- `dotnet build` on `ETStock.slnx` passes (both projects compile)
- All entity files exist with proper C# syntax
- AppDbContext references all entities
- appsettings.json is present with connection string
- ETStock.Tests project exists and builds

## Output: write contract to .team/backend-contract-phase0.md
After completing the work, write `.team/backend-contract-phase0.md` summarizing:
- Packages added and versions
- Entity list with their namespace paths
- AppDbContext location
- Connection string key name
- How to get DbContext from DI (Program.Services)
- Any issues or deviations from this brief
