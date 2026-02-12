using PaymentCore.Application.DTOs;
using PaymentCore.Application.Interfaces;
using PaymentCore.Domain.Entities;
using PaymentCore.Domain.Enums;
using PaymentCore.Domain.Events;
using PaymentCore.Domain.Interfaces;

namespace PaymentCore.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAccountLockService _lockService;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        IAccountLockService lockService)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _lockService = lockService;
    }

    public async Task<TransactionResponse> ProcessTransactionAsync(
        ProcessTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate and parse or find account by ID
        Account? account;
        Guid accountId;
        
        if (Guid.TryParse(request.AccountId, out accountId))
        {
            account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        }
        else
        {
            // Try to find by ExternalId, or create if doesn't exist
            account = await _accountRepository.GetByExternalIdAsync(request.AccountId, cancellationToken);
            if (account == null)
            {
                // Create account with ExternalId
                account = new Account(creditLimit: 0, externalId: request.AccountId);
                await _accountRepository.CreateAsync(account, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            accountId = account.Id;
        }
        
        if (account == null)
        {
            throw new ArgumentException($"Account not found: {request.AccountId}");
        }

        // Check idempotency
        var existingTransaction = await _transactionRepository.GetByReferenceIdAsync(request.ReferenceId, cancellationToken);
        if (existingTransaction != null)
        {
            var existingAccount = await _accountRepository.GetByIdAsync(existingTransaction.AccountId, cancellationToken);
            return await MapToResponseAsync(existingTransaction, existingAccount!, cancellationToken);
        }

        if (!Enum.TryParse<OperationType>(request.Operation, true, out var operationType))
        {
            throw new ArgumentException($"Invalid operation type: {request.Operation}");
        }

        // Convert amount from cents to decimal (BRL)
        var amountInReais = request.Amount / 100m;

        // Parse target account if provided
        Guid? targetAccountId = null;
        if (!string.IsNullOrEmpty(request.TargetAccountId))
        {
            if (Guid.TryParse(request.TargetAccountId, out var parsedTargetId))
            {
                targetAccountId = parsedTargetId;
            }
            else
            {
                // Try to find by ExternalId
                var targetAccount = await _accountRepository.GetByExternalIdAsync(request.TargetAccountId, cancellationToken);
                if (targetAccount != null)
                {
                    targetAccountId = targetAccount.Id;
                }
                else if (operationType == OperationType.Transfer)
                {
                    throw new ArgumentException($"Target account not found: {request.TargetAccountId}");
                }
            }
        }

        // Parse original transaction if provided (for reversal)
        Guid? originalTransactionId = null;
        if (!string.IsNullOrEmpty(request.OriginalTransactionId))
        {
            if (!Guid.TryParse(request.OriginalTransactionId, out var parsedOriginalId))
            {
                throw new ArgumentException($"Invalid original_transaction_id format: {request.OriginalTransactionId}");
            }
            originalTransactionId = parsedOriginalId;
        }

        // Serialize metadata
        string? metadataJson = request.Metadata != null 
            ? System.Text.Json.JsonSerializer.Serialize(request.Metadata) 
            : null;

        var transaction = new Transaction(
            request.ReferenceId,
            operationType,
            amountInReais,
            accountId,
            targetAccountId,
            originalTransactionId,
            metadataJson);

        await _transactionRepository.CreateAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Process transaction with account lock
        await ProcessWithLockAsync(transaction, cancellationToken);

        account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        return await MapToResponseAsync(transaction, account!, cancellationToken);
    }

    private async Task ProcessWithLockAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        var accountIds = transaction.TargetAccountId.HasValue
            ? new[] { transaction.AccountId, transaction.TargetAccountId.Value }.OrderBy(x => x).ToArray()
            : new[] { transaction.AccountId };

        await _lockService.ExecuteWithLockAsync(accountIds, async () =>
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                await ExecuteOperationAsync(transaction, cancellationToken);

                transaction.MarkAsCompleted();
                await _transactionRepository.UpdateAsync(transaction, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Publish event
                var transactionEvent = new TransactionProcessedEvent(
                    transaction.Id,
                    transaction.ReferenceId,
                    transaction.OperationType,
                    transaction.Amount,
                    transaction.AccountId,
                    transaction.TargetAccountId,
                    transaction.Status);

                await _eventPublisher.PublishAsync(transactionEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                transaction.MarkAsFailed(ex.Message);
                await _transactionRepository.UpdateAsync(transaction, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Publish failed event
                var failedEvent = new TransactionProcessedEvent(
                    transaction.Id,
                    transaction.ReferenceId,
                    transaction.OperationType,
                    transaction.Amount,
                    transaction.AccountId,
                    transaction.TargetAccountId,
                    TransactionStatus.Failed,
                    ex.Message);

                await _eventPublisher.PublishAsync(failedEvent, cancellationToken);
            }
        }, cancellationToken);
    }

    private async Task ExecuteOperationAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(transaction.AccountId, cancellationToken)
            ?? throw new InvalidOperationException($"Account {transaction.AccountId} not found");

        switch (transaction.OperationType)
        {
            case OperationType.Credit:
                account.AddCredit(transaction.Amount);
                break;

            case OperationType.Debit:
                account.Debit(transaction.Amount);
                break;

            case OperationType.Reserve:
                account.Reserve(transaction.Amount);
                break;

            case OperationType.Capture:
                account.Capture(transaction.Amount);
                break;

            case OperationType.Transfer:
                if (!transaction.TargetAccountId.HasValue)
                    throw new InvalidOperationException("Target account required for transfer");

                var targetAccount = await _accountRepository.GetByIdAsync(transaction.TargetAccountId.Value, cancellationToken)
                    ?? throw new InvalidOperationException($"Target account {transaction.TargetAccountId.Value} not found");

                account.Debit(transaction.Amount);
                targetAccount.AddCredit(transaction.Amount);
                await _accountRepository.UpdateAsync(targetAccount, cancellationToken);
                break;

            case OperationType.Reversal:
                // Implement reversal logic based on original transaction
                if (!transaction.OriginalTransactionId.HasValue)
                    throw new InvalidOperationException("Original transaction ID is required for reversal");

                var originalTransaction = await _transactionRepository.GetByIdAsync(transaction.OriginalTransactionId.Value, cancellationToken)
                    ?? throw new InvalidOperationException($"Original transaction {transaction.OriginalTransactionId.Value} not found");

                if (originalTransaction.Status != TransactionStatus.Completed)
                    throw new InvalidOperationException($"Cannot reverse transaction with status {originalTransaction.Status}");

                // Reverse the original operation
                switch (originalTransaction.OperationType)
                {
                    case OperationType.Credit:
                        // Reverse credit = debit
                        account.Debit(originalTransaction.Amount);
                        break;

                    case OperationType.Debit:
                        // Reverse debit = credit
                        account.AddCredit(originalTransaction.Amount);
                        break;

                    case OperationType.Reserve:
                        // Reverse reserve = release reservation
                        account.ReleaseReservation(originalTransaction.Amount);
                        break;

                    case OperationType.Capture:
                        // Reverse capture = add back to balance and reserved balance
                        account.AddCredit(originalTransaction.Amount);
                        account.Reserve(originalTransaction.Amount);
                        break;

                    case OperationType.Transfer:
                        // Reverse transfer
                        if (!originalTransaction.TargetAccountId.HasValue)
                            throw new InvalidOperationException("Original transfer has no target account");

                        var originalTargetAccount = await _accountRepository.GetByIdAsync(originalTransaction.TargetAccountId.Value, cancellationToken)
                            ?? throw new InvalidOperationException($"Original target account {originalTransaction.TargetAccountId.Value} not found");

                        // Reverse: debit from target, credit to source
                        originalTargetAccount.Debit(originalTransaction.Amount);
                        account.AddCredit(originalTransaction.Amount);
                        await _accountRepository.UpdateAsync(originalTargetAccount, cancellationToken);
                        break;

                    default:
                        throw new InvalidOperationException($"Cannot reverse transaction of type {originalTransaction.OperationType}");
                }

                // Mark original transaction as reversed
                originalTransaction.MarkAsReversed();
                await _transactionRepository.UpdateAsync(originalTransaction, cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Unsupported operation type: {transaction.OperationType}");
        }

        await _accountRepository.UpdateAsync(account, cancellationToken);
    }

    public async Task<TransactionResponse?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        if (transaction == null) return null;
        
        var account = await _accountRepository.GetByIdAsync(transaction.AccountId, cancellationToken);
        return await MapToResponseAsync(transaction, account!, cancellationToken);
    }

    public async Task<PagedResponse<TransactionResponse>> GetPagedTransactionsAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize > 100 ? 100 : request.PageSize;

        var (items, totalCount) = await _transactionRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var transactionResponses = new List<TransactionResponse>();
        foreach (var transaction in items)
        {
            var account = transaction.Account ?? await _accountRepository.GetByIdAsync(transaction.AccountId, cancellationToken);
            if (account != null)
            {
                var response = await MapToResponseAsync(transaction, account, cancellationToken);
                transactionResponses.Add(response);
            }
        }

        return new PagedResponse<TransactionResponse>
        {
            Items = transactionResponses,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private static Task<TransactionResponse> MapToResponseAsync(Transaction transaction, Account account, CancellationToken cancellationToken = default)
    {
        // Convert from decimal (reais) to long (cents)
        var balanceInCents = (long)(account.Balance * 100);
        var reservedBalanceInCents = (long)(account.ReservedBalance * 100);
        var availableBalanceInCents = (long)(account.AvailableBalance * 100);

        var status = transaction.Status switch
        {
            TransactionStatus.Completed => "success",
            TransactionStatus.Failed => "failed",
            TransactionStatus.Pending => "pending",
            _ => "pending"
        };

        return Task.FromResult(new TransactionResponse
        {
            TransactionId = transaction.Id.ToString(),
            Status = status,
            Balance = balanceInCents,
            ReservedBalance = reservedBalanceInCents,
            AvailableBalance = availableBalanceInCents,
            Timestamp = transaction.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ErrorMessage = transaction.ErrorMessage
        });
    }
}
