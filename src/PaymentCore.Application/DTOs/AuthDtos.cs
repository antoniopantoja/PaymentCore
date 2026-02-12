using System.ComponentModel.DataAnnotations;

namespace PaymentCore.Application.DTOs;

/// <summary>
/// Request para registro de novo usuário
/// </summary>
public record RegisterRequest
{
    /// <summary>
    /// Nome de usuário único
    /// </summary>
    [Required(ErrorMessage = "Username é obrigatório")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username deve ter entre 3 e 50 caracteres")]
    public string Username { get; init; } = null!;

    /// <summary>
    /// Email do usuário
    /// </summary>
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; init; } = null!;

    /// <summary>
    /// Senha (mínimo 6 caracteres)
    /// </summary>
    [Required(ErrorMessage = "Password é obrigatória")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password deve ter no mínimo 6 caracteres")]
    public string Password { get; init; } = null!;

    /// <summary>
    /// Role do usuário (User ou Admin)
    /// </summary>
    public string Role { get; init; } = "User";
}

/// <summary>
/// Request para login de usuário
/// </summary>
public record LoginRequest
{
    /// <summary>
    /// Nome de usuário ou email
    /// </summary>
    [Required(ErrorMessage = "Username ou Email é obrigatório")]
    public string UsernameOrEmail { get; init; } = null!;

    /// <summary>
    /// Senha do usuário
    /// </summary>
    [Required(ErrorMessage = "Password é obrigatória")]
    public string Password { get; init; } = null!;
}

/// <summary>
/// Resposta de autenticação com token JWT
/// </summary>
public record AuthResponse
{
    /// <summary>
    /// Token JWT para autenticação
    /// </summary>
    public string Token { get; init; } = null!;

    /// <summary>
    /// Tipo do token (Bearer)
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Data de expiração do token
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Informações do usuário
    /// </summary>
    public UserInfo User { get; init; } = null!;
}

/// <summary>
/// Informações básicas do usuário
/// </summary>
public record UserInfo
{
    public Guid Id { get; init; }
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Role { get; init; } = null!;
}
