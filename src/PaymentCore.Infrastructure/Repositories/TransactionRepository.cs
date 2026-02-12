using Microsoft.EntityFrameworkCore;
using PaymentCore.Domain.Entities;
using PaymentCore.Domain.Enums;
using PaymentCore.Domain.Interfaces;
using PaymentCore.Infrastructure.Persistence;

namespace PaymentCore.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.TargetAccount)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Transaction?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.TargetAccount)
            .FirstOrDefaultAsync(t => t.ReferenceId == referenceId, cancellationToken);
    }

    public async Task<Transaction> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    public Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Update(transaction);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Transaction>> GetPendingTransactionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Where(t => t.Status == TransactionStatus.Pending)
            .OrderBy(t => t.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Transaction> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .Include(t => t.Account)
            .Include(t => t.TargetAccount)
            .OrderByDescending(t => t.CreatedAt);
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        
        return (items, totalCount);
    }
}
