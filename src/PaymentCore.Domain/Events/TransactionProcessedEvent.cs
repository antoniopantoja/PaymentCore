using PaymentCore.Domain.Enums;

namespace PaymentCore.Domain.Events;

public class TransactionProcessedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public string ReferenceId { get; }
    public OperationType OperationType { get; }
    public decimal Amount { get; }
    public Guid AccountId { get; }
    public Guid? TargetAccountId { get; }
    public TransactionStatus Status { get; }
    public string? ErrorMessage { get; }

    public TransactionProcessedEvent(
        Guid transactionId,
        string referenceId,
        OperationType operationType,
        decimal amount,
        Guid accountId,
        Guid? targetAccountId,
        TransactionStatus status,
        string? errorMessage = null)
    {
        TransactionId = transactionId;
        ReferenceId = referenceId;
        OperationType = operationType;
        Amount = amount;
        AccountId = accountId;
        TargetAccountId = targetAccountId;
        Status = status;
        ErrorMessage = errorMessage;
    }
}
