using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace PaymentCore.Application.DTOs;

/// <summary>
/// Request para criar uma nova conta
/// </summary>
public record CreateAccountRequest
{
    /// <summary>
    /// ID externo da conta (identificador do cliente)
    /// </summary>
    /// <example>CLIENTE-001</example>
    [Description("ID externo da conta")]
    public string? ExternalId { get; init; }
    
    /// <summary>
    /// Limite de crédito da conta em reais
    /// </summary>
    /// <example>5000.00</example>
    [Description("Limite de crédito em R$")]
    public decimal CreditLimit { get; init; } = 0;
}

/// <summary>
/// Resposta com dados da conta
/// </summary>
public record AccountResponse
{
    /// <summary>
    /// ID único da conta
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// ID externo da conta
    /// </summary>
    public string? ExternalId { get; init; }
    
    /// <summary>
    /// Saldo total da conta em reais
    /// </summary>
    public decimal Balance { get; init; }
    
    /// <summary>
    /// Saldo reservado (bloqueado) em reais
    /// </summary>
    public decimal ReservedBalance { get; init; }
    
    /// <summary>
    /// Saldo disponível (Balance - ReservedBalance) em reais
    /// </summary>
    public decimal AvailableBalance { get; init; }
    
    /// <summary>
    /// Limite de crédito da conta em reais
    /// </summary>
    public decimal CreditLimit { get; init; }
    
    /// <summary>
    /// Status da conta (Active, Suspended, Closed)
    /// </summary>
    public string Status { get; init; } = null!;
    
    /// <summary>
    /// Data e hora de criação da conta
    /// </summary>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// Data e hora da última atualização
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Parâmetros para paginação
/// </summary>
public record PaginationRequest
{
    /// <summary>
    /// Número da página (inicia em 1)
    /// </summary>
    public int PageNumber { get; init; } = 1;
    
    /// <summary>
    /// Quantidade de itens por página
    /// </summary>
    public int PageSize { get; init; } = 10;
}

/// <summary>
/// Resposta paginada genérica
/// </summary>
public record PagedResponse<T>
{
    /// <summary>
    /// Lista de itens da página atual
    /// </summary>
    public List<T> Items { get; init; } = new();
    
    /// <summary>
    /// Número da página atual
    /// </summary>
    public int PageNumber { get; init; }
    
    /// <summary>
    /// Quantidade de itens por página
    /// </summary>
    public int PageSize { get; init; }
    
    /// <summary>
    /// Total de itens (todas as páginas)
    /// </summary>
    public int TotalCount { get; init; }
    
    /// <summary>
    /// Total de páginas
    /// </summary>
    public int TotalPages { get; init; }
    
    /// <summary>
    /// Indica se existe página anterior
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
    
    /// <summary>
    /// Indica se existe próxima página
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
