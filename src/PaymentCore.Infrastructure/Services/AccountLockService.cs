using System.Collections.Concurrent;
using PaymentCore.Application.Interfaces;

namespace PaymentCore.Infrastructure.Services;

public class AccountLockService : IAccountLockService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task ExecuteWithLockAsync(Guid[] accountIds, Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var orderedIds = accountIds.OrderBy(x => x).ToArray();
        var semaphores = orderedIds.Select(id => _locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1))).ToArray();

        foreach (var semaphore in semaphores)
        {
            await semaphore.WaitAsync(cancellationToken);
        }

        try
        {
            await operation();
        }
        finally
        {
            foreach (var semaphore in semaphores.Reverse())
            {
                semaphore.Release();
            }
        }
    }
}
