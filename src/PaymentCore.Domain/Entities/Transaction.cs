using PaymentCore.Domain.Enums;

namespace PaymentCore.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public string ReferenceId { get; private set; } = null!;
    public OperationType OperationType { get; private set; }
    public decimal Amount { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? TargetAccountId { get; private set; }
    public Guid? OriginalTransactionId { get; private set; }
    public string? Metadata { get; private set; }
    public DateTime Timestamp { get; private set; }
    public TransactionStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public Account Account { get; private set; } = null!;
    public Account? TargetAccount { get; private set; }

    // EF Core constructor
    private Transaction() { }

    public Transaction(
        string referenceId,
        OperationType operationType,
        decimal amount,
        Guid accountId,
        Guid? targetAccountId = null,
        Guid? originalTransactionId = null,
        string? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(referenceId))
            throw new ArgumentException("Reference ID cannot be empty", nameof(referenceId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (operationType == OperationType.Transfer && !targetAccountId.HasValue)
            throw new ArgumentException("Target account is required for transfer operations", nameof(targetAccountId));

        if (operationType == OperationType.Reversal && !originalTransactionId.HasValue)
            throw new ArgumentException("Original transaction ID is required for reversal operations", nameof(originalTransactionId));

        Id = Guid.NewGuid();
        ReferenceId = referenceId;
        OperationType = operationType;
        Amount = amount;
        AccountId = accountId;
        TargetAccountId = targetAccountId;
        OriginalTransactionId = originalTransactionId;
        Metadata = metadata;
        Timestamp = DateTime.UtcNow;
        Status = TransactionStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompleted()
    {
        Status = TransactionStatus.Completed;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = TransactionStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void MarkAsReversed()
    {
        Status = TransactionStatus.Reversed;
    }
}
