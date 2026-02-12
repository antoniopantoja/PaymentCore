using FluentAssertions;
using PaymentCore.Domain.Entities;
using PaymentCore.Domain.Enums;
using Xunit;

namespace PaymentCore.UnitTests.Domain;

public class AccountTests
{
    [Fact]
    public void Account_ShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var account = new Account(creditLimit: 1000);

        // Assert
        account.Balance.Should().Be(0);
        account.ReservedBalance.Should().Be(0);
        account.CreditLimit.Should().Be(1000);
        account.Status.Should().Be(AccountStatus.Active);
        account.AvailableBalance.Should().Be(0);
    }

    [Fact]
    public void AddCredit_ShouldIncreaseBalance()
    {
        // Arrange
        var account = new Account();

        // Act
        account.AddCredit(100);

        // Assert
        account.Balance.Should().Be(100);
        account.AvailableBalance.Should().Be(100);
    }

    [Fact]
    public void AddCredit_WithNegativeAmount_ShouldThrowException()
    {
        // Arrange
        var account = new Account();

        // Act
        Action act = () => account.AddCredit(-100);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Amount must be greater than zero");
    }

    [Fact]
    public void Debit_WithSufficientBalance_ShouldDecreaseBalance()
    {
        // Arrange
        var account = new Account();
        account.AddCredit(200);

        // Act
        account.Debit(100);

        // Assert
        account.Balance.Should().Be(100);
    }

    [Fact]
    public void Debit_WithCreditLimit_ShouldAllowOverdraft()
    {
        // Arrange
        var account = new Account(creditLimit: 500);
        account.AddCredit(100);

        // Act
        account.Debit(400); // Uses 100 balance + 300 credit

        // Assert
        account.Balance.Should().Be(-300);
    }

    [Fact]
    public void Debit_WithInsufficientFunds_ShouldThrowException()
    {
        // Arrange
        var account = new Account(creditLimit: 100);
        account.AddCredit(50);

        // Act
        Action act = () => account.Debit(200); // Needs 200 but has only 150

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient funds");
    }

    [Fact]
    public void Reserve_ShouldMoveToReservedBalance()
    {
        // Arrange
        var account = new Account();
        account.AddCredit(200);

        // Act
        account.Reserve(100);

        // Assert
        account.Balance.Should().Be(200);
        account.ReservedBalance.Should().Be(100);
        account.AvailableBalance.Should().Be(100);
    }

    [Fact]
    public void Reserve_WithInsufficientAvailableBalance_ShouldThrowException()
    {
        // Arrange
        var account = new Account();
        account.AddCredit(100);
        account.Reserve(50);

        // Act
        Action act = () => account.Reserve(100); // Only 50 available

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient available balance");
    }

    [Fact]
    public void Capture_ShouldUseReservedBalance()
    {
        // Arrange
        var account = new Account();
        account.AddCredit(200);
        account.Reserve(100);

        // Act
        account.Capture(50);

        // Assert
        account.Balance.Should().Be(150);
        account.ReservedBalance.Should().Be(50);
        account.AvailableBalance.Should().Be(100);
    }

    [Fact]
    public void Capture_WithInsufficientReservedBalance_ShouldThrowException()
    {
        // Arrange
        var account = new Account();
        account.AddCredit(200);
        account.Reserve(50);

        // Act
        Action act = () => account.Capture(100);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient reserved balance");
    }

    [Fact]
    public void ReleaseReservation_ShouldDecreaseReservedBalance()
    {
        // Arrange
        var account = new Account();
        account.AddCredit(200);
        account.Reserve(100);

        // Act
        account.ReleaseReservation(50);

        // Assert
        account.Balance.Should().Be(200);
        account.ReservedBalance.Should().Be(50);
        account.AvailableBalance.Should().Be(150);
    }
}
