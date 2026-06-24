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
