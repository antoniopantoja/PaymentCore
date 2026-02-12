using PaymentCore.Application.DTOs;
using PaymentCore.Application.Interfaces;
using PaymentCore.Domain.Entities;
using PaymentCore.Domain.Interfaces;

namespace PaymentCore.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = new Account(request.CreditLimit, request.ExternalId);
        
        await _accountRepository.CreateAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(account);
    }

    public async Task<AccountResponse?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, cancellationToken);
        return account != null ? MapToResponse(account) : null;
    }

    public async Task<List<AccountResponse>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.GetAllAsync(cancellationToken);
        return accounts.Select(MapToResponse).ToList();
    }

    public async Task<PagedResponse<AccountResponse>> GetPagedAccountsAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize > 100 ? 100 : request.PageSize;

        var (items, totalCount) = await _accountRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<AccountResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private static AccountResponse MapToResponse(Account account)
    {
        return new AccountResponse
        {
            Id = account.Id,
            ExternalId = account.ExternalId,
            Balance = account.Balance,
            ReservedBalance = account.ReservedBalance,
            AvailableBalance = account.AvailableBalance,
            CreditLimit = account.CreditLimit,
            Status = account.Status.ToString(),
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }
}
