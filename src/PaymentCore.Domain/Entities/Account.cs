using PaymentCore.Domain.Enums;

namespace PaymentCore.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public string? ExternalId { get; private set; }
    public decimal Balance { get; private set; }
    public decimal ReservedBalance { get; private set; }
    public decimal CreditLimit { get; private set; }
    public AccountStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core constructor
    private Account() { }

    public Account(decimal creditLimit = 0, string? externalId = null)
    {
        Id = Guid.NewGuid();
        ExternalId = externalId;
        Balance = 0;
        ReservedBalance = 0;
        CreditLimit = creditLimit;
        Status = AccountStatus.Active;
        RowVersion = new byte[] { 0 };
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal AvailableBalance => Balance - ReservedBalance;

    public void AddCredit(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero");

        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Account is not active");

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Debit(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero");

        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Account is not active");

        var availableFunds = AvailableBalance + CreditLimit;
        if (amount > availableFunds)
            throw new InvalidOperationException("Insufficient funds");

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reserve(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero");

        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Account is not active");

        if (amount > AvailableBalance)
            throw new InvalidOperationException("Insufficient available balance");

        ReservedBalance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Capture(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero");

        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Account is not active");

        if (amount > ReservedBalance)
            throw new InvalidOperationException("Insufficient reserved balance");

        ReservedBalance -= amount;
        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseReservation(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero");

        if (amount > ReservedBalance)
            throw new InvalidOperationException("Invalid reservation amount");

        ReservedBalance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(AccountStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
}
