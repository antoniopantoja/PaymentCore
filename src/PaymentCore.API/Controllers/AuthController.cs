using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentCore.Application.DTOs;
using PaymentCore.Application.Interfaces;

namespace PaymentCore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Registra um novo usuário no sistema
    /// </summary>
    /// <param name="request">Dados do usuário para registro</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Token JWT e informações do usuário</returns>
    /// <remarks>
    /// Exemplo de requisição:
    /// 
    ///     POST /api/auth/register
    ///     {
    ///        "username": "joao_silva",
    ///        "email": "joao@email.com",
    ///        "password": "senha123",
    ///        "role": "User"
    ///     }
    ///     
    /// </remarks>
    /// <response code="200">Usuário registrado com sucesso</response>
    /// <response code="400">Dados inválidos ou usuário já existe</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Registrando novo usuário: {Username}", request.Username);
            var response = await _authService.RegisterAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Falha ao registrar usuário: {Username}", request.Username);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Realiza login de um usuário existente
    /// </summary>
    /// <param name="request">Credenciais de login</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Token JWT e informações do usuário</returns>
    /// <remarks>
    /// Exemplo de requisição:
    /// 
    ///     POST /api/auth/login
    ///     {
    ///        "usernameOrEmail": "joao_silva",
    ///        "password": "senha123"
    ///     }
    ///     
    /// Você pode usar username ou email no campo usernameOrEmail.
    /// </remarks>
    /// <response code="200">Login realizado com sucesso</response>
    /// <response code="401">Credenciais inválidas</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Tentativa de login: {UsernameOrEmail}", request.UsernameOrEmail);
            var response = await _authService.LoginAsync(request, cancellationToken);
            _logger.LogInformation("Login bem-sucedido: {UsernameOrEmail}", request.UsernameOrEmail);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Falha no login: {UsernameOrEmail}", request.UsernameOrEmail);
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint protegido para testar autenticação
    /// </summary>
    /// <returns>Informações do usuário autenticado</returns>
    /// <response code="200">Usuário autenticado</response>
    /// <response code="401">Não autenticado</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<object> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.Identity?.Name;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new
        {
            userId,
            username,
            email,
            role,
            isAuthenticated = User.Identity?.IsAuthenticated ?? false
        });
    }
}
