namespace PaymentCore.Application.Interfaces;

public interface IAccountLockService
{
    Task ExecuteWithLockAsync(Guid[] accountIds, Func<Task> operation, CancellationToken cancellationToken = default);
}
