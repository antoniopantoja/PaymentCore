using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentCore.Application.DTOs;
using PaymentCore.Application.Interfaces;

namespace PaymentCore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionService transactionService,
        ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    /// <summary>
    /// Processa uma nova transação financeira
    /// </summary>
    /// <param name="request">Dados da transação</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado do processamento da transação</returns>
    /// <remarks>
    /// **Operações disponíveis:**
    /// 
    /// - **credit**: Adiciona saldo (depósito)
    /// - **debit**: Remove saldo (compra/débito)
    /// - **reserve**: Reserva saldo (pré-autorização)
    /// - **capture**: Captura reserva (requer original_transaction_id)
    /// - **reversal**: Estorna transação (requer original_transaction_id)
    /// - **transfer**: Transfere entre contas (requer target_account_id)
    /// 
    /// **Exemplo de CRÉDITO (depósito de R$ 1.000,00):**
    /// 
    ///     POST /api/transactions
    ///     {
    ///        "operation": "credit",
    ///        "account_id": "85d7a1d9-68b4-4e62-8957-556adbf8d996",
    ///        "amount": 100000,
    ///        "currency": "BRL",
    ///        "reference_id": "DEP-001-20260212123045",
    ///        "metadata": {
    ///          "description": "Depósito inicial"
    ///        }
    ///     }
    ///     
    /// **Exemplo de DÉBITO (compra de R$ 150,00):**
    /// 
    ///     POST /api/transactions
    ///     {
    ///        "operation": "debit",
    ///        "account_id": "85d7a1d9-68b4-4e62-8957-556adbf8d996",
    ///        "amount": 15000,
    ///        "currency": "BRL",
    ///        "reference_id": "COMPRA-001-20260212123046"
    ///     }
    ///     
    /// **Exemplo de TRANSFERÊNCIA (R$ 100,00):**
    /// 
    ///     POST /api/transactions
    ///     {
    ///        "operation": "transfer",
    ///        "account_id": "85d7a1d9-68b4-4e62-8957-556adbf8d996",
    ///        "amount": 10000,
    ///        "currency": "BRL",
    ///        "reference_id": "TRF-001-20260212123047",
    ///        "target_account_id": "13edef1a-ee42-49c6-be4d-7b0e4ae3ba15"
    ///     }
    ///     
    /// **IMPORTANTE:**
    /// - Valores em centavos: R$ 100,00 = 10000
    /// - reference_id deve ser único (idempotência)
    /// </remarks>
    /// <response code="201">Transação processada com sucesso (nova)</response>
    /// <response code="200">Transação já processada (idempotência)</response>
    /// <response code="400">Dados inválidos ou saldo insuficiente</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionResponse>> ProcessTransaction(
        [FromBody] ProcessTransactionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing transaction - ReferenceId: {ReferenceId}, Type: {Operation}, Amount: {Amount}",
            request.ReferenceId,
            request.Operation,
            request.Amount);

        var transaction = await _transactionService.ProcessTransactionAsync(request, cancellationToken);

        // If transaction was already processed (idempotency), return 200 instead of 201
        if (transaction.Status == "success" || transaction.Status == "failed")
        {
            return Ok(transaction);
        }

        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.TransactionId }, transaction);
    }

    /// <summary>
    /// Lista transações com paginação
    /// </summary>
    /// <param name="pageNumber">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista paginada de transações</returns>
    /// <remarks>
    /// Exemplo de requisição:
    /// 
    ///     GET /api/transactions?pageNumber=1&amp;pageSize=10
    ///     
    /// </remarks>
    /// <response code="200">Lista de transações retornada com sucesso</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TransactionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TransactionResponse>>> GetAllTransactions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var request = new PaginationRequest { PageNumber = pageNumber, PageSize = pageSize };
        var pagedTransactions = await _transactionService.GetPagedTransactionsAsync(request, cancellationToken);
        return Ok(pagedTransactions);
    }

    /// <summary>
    /// Consulta uma transação pelo ID
    /// </summary>
    /// <param name="id">ID (GUID) da transação</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados da transação</returns>
    /// <response code="200">Transação encontrada</response>
    /// <response code="404">Transação não encontrada</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetTransaction(
        Guid id,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionService.GetTransactionByIdAsync(id, cancellationToken);

        if (transaction == null)
        {
            return NotFound();
        }

        return Ok(transaction);
    }
}
