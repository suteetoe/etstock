using ETStock.Data.Repositories;
using ETStock.Models;
using ETStock.ViewModels;
using Xunit;

namespace ETStock.Tests;

internal sealed class FakeAbbrInvoiceRepository : IAbbrInvoiceRepository
{
    private readonly List<AbbrInvoice> _store = [];
    private int _nextId = 1;

    public FakeAbbrInvoiceRepository(IEnumerable<AbbrInvoice>? seed = null)
    {
        if (seed is null) return;
        foreach (var inv in seed)
        {
            if (inv.Id == 0) inv.Id = _nextId++;
            _store.Add(inv);
        }
    }

    public Task<List<AbbrInvoice>> GetByPeriodAsync(int taxYear, int taxMonth) =>
        Task.FromResult(_store.Where(i => i.TaxYear == taxYear && i.TaxMonth == taxMonth).ToList());

    public Task<AbbrInvoice?> GetByIdAsync(int id) =>
        Task.FromResult(_store.FirstOrDefault(i => i.Id == id));

    public Task SaveAsync(AbbrInvoice invoice)
    {
        if (invoice.Id == 0)
        {
            invoice.Id = _nextId++;
            _store.Add(invoice);
        }
        else
        {
            var idx = _store.FindIndex(i => i.Id == invoice.Id);
            if (idx >= 0)
                _store[idx] = invoice;
            else
                _store.Add(invoice);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        _store.RemoveAll(i => i.Id == id);
        return Task.CompletedTask;
    }

    public Task DeleteByPeriodAsync(int taxYear, int taxMonth, CancellationToken ct = default)
    {
        _store.RemoveAll(i => i.TaxYear == taxYear && i.TaxMonth == taxMonth);
        return Task.CompletedTask;
    }

    public Task<AbbrInvoiceSummary> GetPeriodSummaryAsync(int taxYear, int taxMonth)
    {
        var matching = _store.Where(i => i.TaxYear == taxYear && i.TaxMonth == taxMonth).ToList();
        var summary = new AbbrInvoiceSummary(
            matching.Count,
            matching.Sum(i => i.TotalAmount),
            matching.Sum(i => i.VatAmount));
        return Task.FromResult(summary);
    }

    public Task<(int BookNo, int RunningNo)?> GetLatestRunningAsync(CancellationToken ct = default)
    {
        var latest = _store
            .Where(i => i.RunningNo != null)
            .OrderByDescending(i => i.RunningNo)
            .FirstOrDefault();

        (int BookNo, int RunningNo)? result = latest is null
            ? null
            : (latest.BookNo!.Value, latest.RunningNo!.Value);

        return Task.FromResult(result);
    }

    public Task<int> CountByBookNoAsync(int bookNo, CancellationToken ct = default) =>
        Task.FromResult(_store.Count(i => i.BookNo == bookNo));

    public IReadOnlyList<AbbrInvoice> All => _store.AsReadOnly();
}

public class InvoiceViewModelTests
{
    private static AbbrInvoice MakeInvoice(int taxYear, int taxMonth, string invoiceNo = "INV-001") =>
        new()
        {
            InvoiceNo = invoiceNo,
            InvoiceDate = DateTime.Today,
            TaxYear = taxYear,
            TaxMonth = taxMonth,
            TotalAmount = 107m,
            VatAmount = 7m,
            Items = [new AbbrInvoiceItem { ProductName = "สินค้า ก", Qty = 1, Amount = 100m, VatAmount = 7m }],
        };

    // ── Default state ──────────────────────────────────────────────────────────

    [Fact]
    public void DefaultYear_IsThaiYear()
    {
        var vm = new InvoiceViewModel();
        Assert.Equal(DateTime.Today.Year + 543, vm.SelectedYear);
    }

    [Fact]
    public void DefaultMonth_IsCurrentMonth()
    {
        var vm = new InvoiceViewModel();
        Assert.Equal(DateTime.Today.Month, vm.SelectedMonth);
    }

    [Fact]
    public void Invoices_StartsEmpty()
    {
        var vm = new InvoiceViewModel();
        Assert.Empty(vm.Invoices);
    }

    // ── LoadCommand ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_PopulatesInvoices_WhenPeriodHasData()
    {
        var baseVm = new InvoiceViewModel();
        int year = baseVm.SelectedYear;
        int month = baseVm.SelectedMonth;

        var repo = new FakeAbbrInvoiceRepository([
            MakeInvoice(year, month, "INV-001"),
            MakeInvoice(year, month, "INV-002"),
        ]);
        var vm = new InvoiceViewModel(repo);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Invoices.Count);
    }

    [Fact]
    public async Task LoadAsync_UpdatesSummary()
    {
        var baseVm = new InvoiceViewModel();
        int year = baseVm.SelectedYear;
        int month = baseVm.SelectedMonth;

        var repo = new FakeAbbrInvoiceRepository([
            new AbbrInvoice { InvoiceNo = "A", TaxYear = year, TaxMonth = month, TotalAmount = 107m, VatAmount = 7m, Items = [] },
            new AbbrInvoice { InvoiceNo = "B", TaxYear = year, TaxMonth = month, TotalAmount = 214m, VatAmount = 14m, Items = [] },
        ]);
        var vm = new InvoiceViewModel(repo);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.SummaryCount);
        Assert.Equal(321m, vm.SummaryTotal);
        Assert.Equal(21m, vm.SummaryVat);
    }

    [Fact]
    public async Task LoadAsync_ClearsStatusMessage()
    {
        var repo = new FakeAbbrInvoiceRepository();
        var vm = new InvoiceViewModel(repo);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.DoesNotContain("ไม่สามารถ", vm.StatusMessage);
    }

    [Fact]
    public void LoadAsync_WithNoRepository_SetsErrorStatus()
    {
        var vm = new InvoiceViewModel();
        vm.LoadCommand.Execute(null);

        Assert.NotEmpty(vm.StatusMessage);
    }

    // ── CurrentBookNo / CurrentDocNo ───────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_PopulatesCurrentBookNoAndDocNo_WhenRepositoryHasValue()
    {
        var baseVm = new InvoiceViewModel();
        int year = baseVm.SelectedYear;
        int month = baseVm.SelectedMonth;

        var repo = new FakeAbbrInvoiceRepository([
            new AbbrInvoice
            {
                InvoiceNo = "INV-001",
                InvoiceDate = DateTime.Today,
                TaxYear = year,
                TaxMonth = month,
                TotalAmount = 107m,
                VatAmount = 7m,
                BookNo = 3,
                RunningNo = 42,
                Items = [],
            },
        ]);
        var vm = new InvoiceViewModel(repo);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(3, vm.CurrentBookNo);
        Assert.Equal(42, vm.CurrentDocNo);
    }

    [Fact]
    public async Task LoadAsync_LeavesBothZero_WhenRepositoryReturnsNull()
    {
        var repo = new FakeAbbrInvoiceRepository();
        var vm = new InvoiceViewModel(repo);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(0, vm.CurrentBookNo);
        Assert.Equal(0, vm.CurrentDocNo);
        Assert.Contains("ยังไม่มีเลขที่ใบกำกับในระบบ", vm.StatusMessage);
        Assert.True(vm.NeedsSeed);
    }

    // ── ToggleExpandCommand ────────────────────────────────────────────────────

    [Fact]
    public async Task ToggleExpandCommand_TogglesIsExpanded()
    {
        var baseVm = new InvoiceViewModel();
        int year = baseVm.SelectedYear;
        int month = baseVm.SelectedMonth;

        var repo = new FakeAbbrInvoiceRepository([MakeInvoice(year, month, "INV-001")]);
        var vm = new InvoiceViewModel(repo);
        await vm.LoadCommand.ExecuteAsync(null);

        var row = Assert.Single(vm.Invoices);
        Assert.False(row.IsExpanded);

        vm.ToggleExpandCommand.Execute(row);
        Assert.True(row.IsExpanded);

        vm.ToggleExpandCommand.Execute(row);
        Assert.False(row.IsExpanded);
    }
}
