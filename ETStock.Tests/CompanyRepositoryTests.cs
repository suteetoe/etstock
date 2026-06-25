using ETStock.Data;
using ETStock.Data.Repositories;
using ETStock.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ETStock.Tests;

public class CompanyRepositoryTests
{
    private static AppDbContext CreateDb() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenEmpty()
    {
        await using var db = CreateDb();
        var repository = new CompanyRepository(db);

        var company = await repository.GetAsync();

        Assert.Null(company);
    }

    [Fact]
    public async Task UpsertAsync_Inserts_WhenNoRecord()
    {
        await using var db = CreateDb();
        var repository = new CompanyRepository(db);
        var company = CreateCompany();

        await repository.UpsertAsync(company);

        var saved = await db.Companies.SingleAsync();
        AssertCompany(company, saved);
    }

    [Fact]
    public async Task UpsertAsync_Updates_WhenRecordExists()
    {
        await using var db = CreateDb();
        var original = CreateCompany();
        db.Companies.Add(original);
        await db.SaveChangesAsync();

        var repository = new CompanyRepository(db);
        var updated = new Company
        {
            Name = "Updated Company",
            TaxId = "0999999999999",
            Address = "456 Updated Road",
            BranchName = "Updated Branch",
            BranchCode = "00001",
            InvoicePrefix = "UPD",
            VatRate = 0.10m
        };

        await repository.UpsertAsync(updated);

        var saved = await db.Companies.SingleAsync();
        Assert.Equal(original.Id, saved.Id);
        AssertCompany(updated, saved);
    }

    private static Company CreateCompany() => new()
    {
        Name = "Test Company",
        TaxId = "0105566001234",
        Address = "123 Test Road",
        BranchName = "Head Office",
        BranchCode = "00000",
        InvoicePrefix = "INV",
        VatRate = 0.07m
    };

    private static void AssertCompany(Company expected, Company actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.TaxId, actual.TaxId);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.BranchName, actual.BranchName);
        Assert.Equal(expected.BranchCode, actual.BranchCode);
        Assert.Equal(expected.InvoicePrefix, actual.InvoicePrefix);
        Assert.Equal(expected.VatRate, actual.VatRate);
    }
}
