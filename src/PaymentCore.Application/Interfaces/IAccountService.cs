using PaymentCore.Application.DTOs;

namespace PaymentCore.Application.Interfaces;

public interface IAccountService
{
    Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default);
    Task<AccountResponse?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<AccountResponse>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<PagedResponse<AccountResponse>> GetPagedAccountsAsync(PaginationRequest request, CancellationToken cancellationToken = default);
}
