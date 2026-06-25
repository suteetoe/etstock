using ETStock.Data.Repositories;
using ETStock.Models;
using ETStock.Services;
using ETStock.ViewModels;
using Xunit;

namespace ETStock.Tests;

// FakeCompanyRepository is defined in CompanyViewModelTests.cs (same namespace/assembly)

public class InvoicePrintServiceTests
{
    private static AbbrInvoice MakeInvoice(string invoiceNo = "TF-001") => new()
    {
        Id = 1,
        InvoiceNo = invoiceNo,
        InvoiceDate = new DateTime(2025, 6, 1),
        TaxYear = 2568,
        TaxMonth = 6,
        TotalAmount = 107m,
        VatAmount = 7m,
        Items =
        [
            new AbbrInvoiceItem
            {
                ProductId = 10,
                Product = new Product { Code = "P001", Name = "สินค้า ก", Unit = "ชิ้น" },
                Qty = 2m,
                Amount = 100m,
                VatAmount = 7m,
            },
        ],
    };

    private static Company MakeCompany() => new()
    {
        Name = "บริษัท ทดสอบ จำกัด",
        TaxId = "1234567890123",
        Address = "99 ถ.ทดสอบ กรุงเทพฯ",
        BranchName = "สำนักงานใหญ่",
        BranchCode = "00000",
    };

    [Fact]
    public async Task BuildAsync_MapsCompanyFieldsCorrectly()
    {
        var company = MakeCompany();
        var svc = new InvoicePrintService(
            new FakeAbbrInvoiceRepository([MakeInvoice()]),
            new FakeCompanyRepository(company));

        var doc = await svc.BuildAsync(1);

        Assert.Equal(company.Name, doc.CompanyName);
        Assert.Equal(company.TaxId, doc.CompanyTaxId);
        Assert.Equal(company.Address, doc.CompanyAddress);
        Assert.Equal(company.BranchName, doc.CompanyBranch);
        Assert.Equal(company.BranchCode, doc.CompanyBranchCode);
    }

    [Fact]
    public async Task BuildAsync_MapsInvoiceHeaderCorrectly()
    {
        var inv = MakeInvoice("INV-2025-001");
        var svc = new InvoicePrintService(
            new FakeAbbrInvoiceRepository([inv]),
            new FakeCompanyRepository(MakeCompany()));

        var doc = await svc.BuildAsync(1);

        Assert.Equal("INV-2025-001", doc.InvoiceNo);
        Assert.Equal(inv.InvoiceDate, doc.InvoiceDate);
        Assert.Equal(2568, doc.TaxYear);
        Assert.Equal(6, doc.TaxMonth);
    }

    [Fact]
    public async Task BuildAsync_MapsLinesFromItems()
    {
        var svc = new InvoicePrintService(
            new FakeAbbrInvoiceRepository([MakeInvoice()]),
            new FakeCompanyRepository(MakeCompany()));

        var doc = await svc.BuildAsync(1);

        Assert.Single(doc.Lines);
        var line = doc.Lines[0];
        Assert.Equal("P001", line.ProductCode);
        Assert.Equal("สินค้า ก", line.ProductName);
        Assert.Equal(2m, line.Qty);
        Assert.Equal(100m, line.Amount);
        Assert.Equal(7m, line.VatAmount);
    }

    [Fact]
    public async Task BuildAsync_ComputesTotalsCorrectly()
    {
        var svc = new InvoicePrintService(
            new FakeAbbrInvoiceRepository([MakeInvoice()]),
            new FakeCompanyRepository(MakeCompany()));

        var doc = await svc.BuildAsync(1);

        Assert.Equal(100m, doc.SubTotal);
        Assert.Equal(7m, doc.VatTotal);
        Assert.Equal(107m, doc.GrandTotal);
    }

    [Fact]
    public async Task BuildAsync_UsesFallbackProductName_WhenProductIsNull()
    {
        var invoice = new AbbrInvoice
        {
            Id = 1,
            InvoiceNo = "TF-001",
            InvoiceDate = DateTime.Today,
            TaxYear = 2568,
            TaxMonth = 6,
            Items = [new AbbrInvoiceItem { ProductId = 99, Product = null!, Qty = 1, Amount = 50m, VatAmount = 3.5m }],
        };
        var svc = new InvoicePrintService(
            new FakeAbbrInvoiceRepository([invoice]),
            new FakeCompanyRepository());

        var doc = await svc.BuildAsync(1);

        Assert.Equal(string.Empty, doc.Lines[0].ProductCode);
        Assert.Equal("สินค้า 99", doc.Lines[0].ProductName);
    }

    [Fact]
    public async Task BuildAsync_UsesDefaultCompany_WhenCompanyIsNull()
    {
        var svc = new InvoicePrintService(
            new FakeAbbrInvoiceRepository([MakeInvoice()]),
            new FakeCompanyRepository(null));

        var doc = await svc.BuildAsync(1);

        Assert.Equal(string.Empty, doc.CompanyName);
        Assert.Equal(string.Empty, doc.CompanyTaxId);
    }

    [Fact]
    public async Task BuildAsync_ThrowsInvalidOperationException_WhenInvoiceNotFound()
    {
        var svc = new InvoicePrintService(
            new FakeAbbrInvoiceRepository(),
            new FakeCompanyRepository(MakeCompany()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.BuildAsync(999));
    }
}

public class PrintPreviewViewModelTests
{
    private static InvoiceDocumentModel MakeDoc() => new(
        "Company A", "1234567890123", "123 Addr", "สำนักงานใหญ่", "00000",
        "INV-001", new DateTime(2025, 6, 1), 2568, 6,
        [new InvoiceDocumentLine("P001", "สินค้า ก", 2m, 100m, 7m)],
        100m, 7m, 107m);

    [Fact]
    public void Constructor_SetsDocCorrectly()
    {
        var doc = MakeDoc();
        var vm = new PrintPreviewViewModel(doc);
        Assert.Same(doc, vm.Doc);
    }

    [Fact]
    public void CloseCommand_RaisesCloseRequested()
    {
        var vm = new PrintPreviewViewModel(MakeDoc());
        var raised = false;
        vm.CloseRequested += () => raised = true;
        vm.CloseCommand.Execute(null);
        Assert.True(raised);
    }

    [Fact]
    public void BuildHtml_ContainsCompanyName()
    {
        var html = PrintPreviewViewModel.BuildHtml(MakeDoc());
        Assert.Contains("Company A", html);
    }

    [Fact]
    public void BuildHtml_ContainsInvoiceNo()
    {
        var html = PrintPreviewViewModel.BuildHtml(MakeDoc());
        Assert.Contains("INV-001", html);
    }

    [Fact]
    public void BuildHtml_ContainsTaxId()
    {
        var html = PrintPreviewViewModel.BuildHtml(MakeDoc());
        Assert.Contains("1234567890123", html);
    }

    [Fact]
    public void BuildHtml_ContainsProductLine()
    {
        var html = PrintPreviewViewModel.BuildHtml(MakeDoc());
        Assert.Contains("P001", html);
        Assert.Contains("สินค้า ก", html);
    }

    [Fact]
    public void BuildHtml_ContainsVatTotal()
    {
        var html = PrintPreviewViewModel.BuildHtml(MakeDoc());
        Assert.Contains("7.00", html);
    }

    [Fact]
    public void BuildHtml_ContainsGrandTotal()
    {
        var html = PrintPreviewViewModel.BuildHtml(MakeDoc());
        Assert.Contains("107.00", html);
    }
}
