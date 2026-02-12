using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentCore.Application.DTOs;
using PaymentCore.Application.Interfaces;

namespace PaymentCore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IAccountService accountService, ILogger<AccountsController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// Cria uma nova conta
    /// </summary>
    /// <param name="request">Dados da conta a ser criada</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Conta criada com sucesso</returns>
    /// <remarks>
    /// Exemplo de requisição:
    /// 
    ///     POST /api/accounts
    ///     {
    ///        "externalId": "CLIENTE-001",
    ///        "creditLimit": 5000.00
    ///     }
    ///     
    /// </remarks>
    /// <response code="201">Conta criada com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccountResponse>> CreateAccount(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new account with credit limit: {CreditLimit}", request.CreditLimit);

        var account = await _accountService.CreateAccountAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
    }

    /// <summary>
    /// Lista contas com paginação
    /// </summary>
    /// <param name="pageNumber">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista paginada de contas</returns>
    /// <remarks>
    /// Exemplo de requisição:
    /// 
    ///     GET /api/accounts?pageNumber=1&amp;pageSize=10
    ///     
    /// </remarks>
    /// <response code="200">Lista de contas retornada com sucesso</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AccountResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AccountResponse>>> GetAllAccounts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var request = new PaginationRequest { PageNumber = pageNumber, PageSize = pageSize };
        var pagedAccounts = await _accountService.GetPagedAccountsAsync(request, cancellationToken);
        return Ok(pagedAccounts);
    }

    /// <summary>
    /// Consulta uma conta pelo ID
    /// </summary>
    /// <param name="id">ID (GUID) da conta</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados da conta</returns>
    /// <response code="200">Conta encontrada</response>
    /// <response code="404">Conta não encontrada</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponse>> GetAccount(Guid id, CancellationToken cancellationToken)
    {
        var account = await _accountService.GetAccountByIdAsync(id, cancellationToken);

        if (account == null)
        {
            return NotFound();
        }

        return Ok(account);
    }
}
