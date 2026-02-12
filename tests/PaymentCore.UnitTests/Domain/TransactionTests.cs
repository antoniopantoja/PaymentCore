using FluentAssertions;
using PaymentCore.Domain.Entities;
using PaymentCore.Domain.Enums;
using Xunit;

namespace PaymentCore.UnitTests.Domain;

public class TransactionTests
{
    [Fact]
    public void Transaction_ShouldCreateWithValidData()
    {
        // Arrange & Act
        var accountId = Guid.NewGuid();
        var transaction = new Transaction(
            "REF-001",
            OperationType.Credit,
            100,
            accountId);

        // Assert
        transaction.ReferenceId.Should().Be("REF-001");
        transaction.OperationType.Should().Be(OperationType.Credit);
        transaction.Amount.Should().Be(100);
        transaction.AccountId.Should().Be(accountId);
        transaction.Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public void Transaction_WithEmptyReferenceId_ShouldThrowException()
    {
        // Arrange & Act
        Action act = () => new Transaction(
            "",
            OperationType.Credit,
            100,
            Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Reference ID cannot be empty*");
    }

    [Fact]
    public void Transaction_WithNegativeAmount_ShouldThrowException()
    {
        // Arrange & Act
        Action act = () => new Transaction(
            "REF-001",
            OperationType.Credit,
            -100,
            Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Amount must be greater than zero*");
    }

    [Fact]
    public void Transaction_Transfer_WithoutTargetAccount_ShouldThrowException()
    {
        // Arrange & Act
        Action act = () => new Transaction(
            "REF-001",
            OperationType.Transfer,
            100,
            Guid.NewGuid(),
            targetAccountId: null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Target account is required for transfer operations*");
    }

    [Fact]
    public void MarkAsCompleted_ShouldUpdateStatus()
    {
        // Arrange
        var transaction = new Transaction(
            "REF-001",
            OperationType.Credit,
            100,
            Guid.NewGuid());

        // Act
        transaction.MarkAsCompleted();

        // Assert
        transaction.Status.Should().Be(TransactionStatus.Completed);
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatusAndSetErrorMessage()
    {
        // Arrange
        var transaction = new Transaction(
            "REF-001",
            OperationType.Credit,
            100,
            Guid.NewGuid());

        // Act
        transaction.MarkAsFailed("Insufficient funds");

        // Assert
        transaction.Status.Should().Be(TransactionStatus.Failed);
        transaction.ErrorMessage.Should().Be("Insufficient funds");
    }

    [Fact]
    public void MarkAsReversed_ShouldUpdateStatus()
    {
        // Arrange
        var transaction = new Transaction(
            "REF-001",
            OperationType.Credit,
            100,
            Guid.NewGuid());
        transaction.MarkAsCompleted();

        // Act
        transaction.MarkAsReversed();

        // Assert
        transaction.Status.Should().Be(TransactionStatus.Reversed);
    }
}
