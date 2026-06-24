using ETStock.Data;
using ETStock.Data.Repositories;
using ETStock.Models;
using ETStock.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ETStock.Tests;

public class InvoiceViewModelTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task LoadCommand_WithNoInvoices_SetsEmptyCollection()
    {
        var db = CreateDb();
        var repository = new AbbrInvoiceRepository(db);
        var viewModel = new InvoiceViewModel(repository)
        {
            SelectedYear = 2026,
            SelectedMonth = 6
        };

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(0, viewModel.Invoices.Count);
        Assert.Contains("0", viewModel.StatusMessage);
    }

    [Fact]
    public async Task LoadCommand_WithMatchingPeriod_PopulatesInvoices()
    {
        var db = CreateDb();
        db.AbbrInvoices.AddRange(
            new AbbrInvoice
            {
                InvoiceNo = "INV-001",
                InvoiceDate = new DateTime(2026, 6, 1),
                TaxYear = 2026,
                TaxMonth = 6,
                TotalAmount = 1000,
                VatAmount = 70,
                Items = []
            },
            new AbbrInvoice
            {
                InvoiceNo = "INV-002",
                InvoiceDate = new DateTime(2026, 6, 15),
                TaxYear = 2026,
                TaxMonth = 6,
                TotalAmount = 2000,
                VatAmount = 140,
                Items = []
            },
            new AbbrInvoice
            {
                InvoiceNo = "INV-003",
                InvoiceDate = new DateTime(2026, 7, 1),
                TaxYear = 2026,
                TaxMonth = 7,
                TotalAmount = 500,
                VatAmount = 35,
                Items = []
            });
        await db.SaveChangesAsync();

        var repository = new AbbrInvoiceRepository(db);
        var viewModel = new InvoiceViewModel(repository)
        {
            SelectedYear = 2026,
            SelectedMonth = 6
        };

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Invoices.Count);
    }

    [Fact]
    public async Task AddInvoiceCommand_AddsNewInvoiceToDb()
    {
        var db = CreateDb();
        var repository = new AbbrInvoiceRepository(db);
        var viewModel = new InvoiceViewModel(repository)
        {
            SelectedYear = 2026,
            SelectedMonth = 6
        };

        await viewModel.AddInvoiceCommand.ExecuteAsync(null);
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(1, viewModel.Invoices.Count);
        Assert.Equal("NEW", viewModel.Invoices[0].InvoiceNo);
    }

    [Fact]
    public async Task DeleteCommand_RemovesSelectedInvoice()
    {
        var db = CreateDb();
        db.AbbrInvoices.AddRange(
            new AbbrInvoice
            {
                InvoiceNo = "INV-001",
                InvoiceDate = new DateTime(2026, 6, 1),
                TaxYear = 2026,
                TaxMonth = 6,
                TotalAmount = 1000,
                VatAmount = 70,
                Items = []
            },
            new AbbrInvoice
            {
                InvoiceNo = "INV-002",
                InvoiceDate = new DateTime(2026, 6, 15),
                TaxYear = 2026,
                TaxMonth = 6,
                TotalAmount = 2000,
                VatAmount = 140,
                Items = []
            });
        await db.SaveChangesAsync();

        var repository = new AbbrInvoiceRepository(db);
        var viewModel = new InvoiceViewModel(repository)
        {
            SelectedYear = 2026,
            SelectedMonth = 6
        };

        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedInvoice = viewModel.Invoices[0];
        await viewModel.DeleteCommand.ExecuteAsync(null);

        Assert.Equal(1, viewModel.Invoices.Count);
    }

    [Fact]
    public async Task DeleteCommand_WithNoSelection_DoesNothing()
    {
        var db = CreateDb();
        db.AbbrInvoices.Add(new AbbrInvoice
        {
            InvoiceNo = "INV-001",
            InvoiceDate = new DateTime(2026, 6, 1),
            TaxYear = 2026,
            TaxMonth = 6,
            TotalAmount = 1000,
            VatAmount = 70,
            Items = []
        });
        await db.SaveChangesAsync();

        var repository = new AbbrInvoiceRepository(db);
        var viewModel = new InvoiceViewModel(repository)
        {
            SelectedYear = 2026,
            SelectedMonth = 6
        };

        await viewModel.LoadCommand.ExecuteAsync(null);
        // Do NOT set SelectedInvoice
        await viewModel.DeleteCommand.ExecuteAsync(null);

        Assert.Equal(1, viewModel.Invoices.Count);
        Assert.DoesNotContain("ลบ", viewModel.StatusMessage);
    }

    [Fact]
    public async Task LoadCommand_NoRepository_SetsStatusMessage()
    {
        var viewModel = new InvoiceViewModel();

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(string.IsNullOrEmpty(viewModel.StatusMessage));
    }
}
