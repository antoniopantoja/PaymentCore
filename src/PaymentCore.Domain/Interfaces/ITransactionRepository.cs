using PaymentCore.Domain.Entities;

namespace PaymentCore.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);
    Task<Transaction> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetPendingTransactionsAsync(CancellationToken cancellationToken = default);
    Task<(List<Transaction> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
