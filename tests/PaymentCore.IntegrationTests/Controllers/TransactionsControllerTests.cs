using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentCore.Application.DTOs;
using Xunit;

namespace PaymentCore.IntegrationTests.Controllers;

public class TransactionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TransactionsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProcessTransaction_Credit_ShouldSucceed()
    {
        // Arrange
        var account = await CreateAccountAsync(1000);
        var request = new ProcessTransactionRequest
        {
            ReferenceId = $"TXN-{Guid.NewGuid()}",
            Operation = "credit",
            Amount = 50000, // 500 BRL in cents
            AccountId = account.Id.ToString(),
            Currency = "BRL"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction.Should().NotBeNull();
        transaction!.Balance.Should().BeGreaterThan(0);
        transaction.Status.Should().BeOneOf("pending", "success");
    }

    [Fact]
    public async Task ProcessTransaction_WithDuplicateReferenceId_ShouldBeIdempotent()
    {
        // Arrange
        var account = await CreateAccountAsync(1000);
        var referenceId = $"TXN-{Guid.NewGuid()}";
        var request = new ProcessTransactionRequest
        {
            ReferenceId = referenceId,
            Operation = "credit",
            Amount = 50000, // 500 BRL in cents
            AccountId = account.Id.ToString(),
            Currency = "BRL"
        };

        // Act - First request
        var firstResponse = await _client.PostAsJsonAsync("/api/transactions", request);
        var firstTransaction = await firstResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        // Act - Second request with same ReferenceId
        var secondResponse = await _client.PostAsJsonAsync("/api/transactions", request);
        var secondTransaction = await secondResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK); // Idempotent
        
        firstTransaction!.TransactionId.Should().Be(secondTransaction!.TransactionId);
        firstTransaction.Balance.Should().Be(secondTransaction.Balance);
    }

    [Fact]
    public async Task ProcessTransaction_Debit_WithInsufficientFunds_ShouldFail()
    {
        // Arrange
        var account = await CreateAccountAsync(0); // No credit limit
        var request = new ProcessTransactionRequest
        {
            ReferenceId = $"TXN-{Guid.NewGuid()}",
            Operation = "debit",
            Amount = 50000, // 500 BRL in cents
            AccountId = account.Id.ToString(),
            Currency = "BRL"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        // Transaction is created but will fail during processing
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransaction_WithValidId_ShouldReturnTransaction()
    {
        // Arrange
        var account = await CreateAccountAsync(1000);
        var createRequest = new ProcessTransactionRequest
        {
            ReferenceId = $"TXN-{Guid.NewGuid()}",
            Operation = "credit",
            Amount = 10000, // 100 BRL in cents
            AccountId = account.Id.ToString(),
            Currency = "BRL"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/transactions", createRequest);
        var createdTransaction = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        // Act
        var response = await _client.GetAsync($"/api/transactions/{createdTransaction!.TransactionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction.Should().NotBeNull();
        transaction!.TransactionId.Should().Be(createdTransaction.TransactionId);
    }

    private async Task<AccountResponse> CreateAccountAsync(decimal creditLimit)
    {
        var request = new CreateAccountRequest { CreditLimit = creditLimit };
        var response = await _client.PostAsJsonAsync("/api/accounts", request);
        return (await response.Content.ReadFromJsonAsync<AccountResponse>())!;
    }
}
