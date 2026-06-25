using ETStock.Models;

namespace ETStock.Data.Repositories;

public interface ICompanyRepository
{
    Task<Company?> GetAsync(CancellationToken ct = default);
    Task UpsertAsync(Company company, CancellationToken ct = default);
}
