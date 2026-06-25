using ETStock.Data.Repositories;
using ETStock.Models;
using ETStock.ViewModels;
using Xunit;

namespace ETStock.Tests;

internal class FakeCompanyRepository(Company? initial = null) : ICompanyRepository
{
    private Company? _stored = initial;
    public Task<Company?> GetAsync(CancellationToken ct = default) => Task.FromResult(_stored);
    public Task UpsertAsync(Company company, CancellationToken ct = default)
    {
        _stored = company;
        return Task.CompletedTask;
    }
}

public class CompanyViewModelTests
{
    [Fact]
    public async Task LoadAsync_PopulatesProperties_WhenCompanyExists()
    {
        var repo = new FakeCompanyRepository(new Company
        {
            Name = "บริษัท ทดสอบ",
            TaxId = "0105566001234",
            VatRate = 0.07m,
            InvoicePrefix = "AB"
        });
        var vm = new CompanyViewModel(repo);
        await vm.LoadCommand.ExecuteAsync(null);
        Assert.Equal("บริษัท ทดสอบ", vm.Name);
        Assert.Equal("0105566001234", vm.TaxId);
        Assert.Equal("AB", vm.InvoicePrefix);
    }

    [Fact]
    public async Task SaveAsync_CallsUpsert_WithCorrectValues()
    {
        var repo = new FakeCompanyRepository();
        var vm = new CompanyViewModel(repo)
        {
            Name = "บริษัท ทดสอบ",
            TaxId = "0105566001234",
            Address = "123 ถนนทดสอบ",
            VatRate = "7"
        };
        await vm.SaveCommand.ExecuteAsync(null);
        var saved = await repo.GetAsync();
        Assert.NotNull(saved);
        Assert.Equal("บริษัท ทดสอบ", saved.Name);
        Assert.Equal(0.07m, saved.VatRate);
    }

    [Fact]
    public async Task LoadAsync_SetsNoProperties_WhenNoCompanyRecord()
    {
        var repo = new FakeCompanyRepository(null);
        var vm = new CompanyViewModel(repo);
        await vm.LoadCommand.ExecuteAsync(null);
        Assert.Equal(string.Empty, vm.Name);
        Assert.Equal(string.Empty, vm.TaxId);
    }
}
