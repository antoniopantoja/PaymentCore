using PaymentCore.Domain.Entities;

namespace PaymentCore.Domain.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Account?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<List<Account>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(List<Account> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Account> CreateAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
