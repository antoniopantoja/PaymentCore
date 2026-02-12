using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentCore.Application.DTOs;
using Xunit;

namespace PaymentCore.IntegrationTests.Controllers;

public class AccountsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AccountsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateAccount_ShouldReturnCreatedAccount()
    {
        // Arrange
        var request = new CreateAccountRequest { CreditLimit = 1000 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        account.Should().NotBeNull();
        account!.CreditLimit.Should().Be(1000);
        account.Balance.Should().Be(0);
        account.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetAccount_WithValidId_ShouldReturnAccount()
    {
        // Arrange
        var createRequest = new CreateAccountRequest { CreditLimit = 500 };
        var createResponse = await _client.PostAsJsonAsync("/api/accounts", createRequest);
        var createdAccount = await createResponse.Content.ReadFromJsonAsync<AccountResponse>();

        // Act
        var response = await _client.GetAsync($"/api/accounts/{createdAccount!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        account.Should().NotBeNull();
        account!.Id.Should().Be(createdAccount.Id);
        account.CreditLimit.Should().Be(500);
    }

    [Fact]
    public async Task GetAccount_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/accounts/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
