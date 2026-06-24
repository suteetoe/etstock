using ETStock.Models;
using Microsoft.EntityFrameworkCore;

namespace ETStock.Data.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly AppDbContext _db;

    public CompanyRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Company?> GetAsync(CancellationToken ct = default)
    {
        return await _db.Companies.FirstOrDefaultAsync(ct);
    }

    public async Task UpsertAsync(Company company, CancellationToken ct = default)
    {
        var existing = await _db.Companies.FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            _db.Companies.Add(company);
        }
        else
        {
            existing.Name = company.Name;
            existing.TaxId = company.TaxId;
            existing.Address = company.Address;
            existing.BranchName = company.BranchName;
            existing.BranchCode = company.BranchCode;
            existing.InvoicePrefix = company.InvoicePrefix;
            existing.VatRate = company.VatRate;
        }

        await _db.SaveChangesAsync(ct);
    }
}
