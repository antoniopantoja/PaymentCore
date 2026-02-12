using PaymentCore.Application.DTOs;

namespace PaymentCore.Application.Interfaces;

public interface ITransactionService
{
    Task<TransactionResponse> ProcessTransactionAsync(ProcessTransactionRequest request, CancellationToken cancellationToken = default);
    Task<TransactionResponse?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResponse<TransactionResponse>> GetPagedTransactionsAsync(PaginationRequest request, CancellationToken cancellationToken = default);
}
