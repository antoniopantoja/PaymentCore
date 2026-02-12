using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace PaymentCore.Application.DTOs;

/// <summary>
/// Request para processar uma transação
/// </summary>
public record ProcessTransactionRequest
{
    /// <summary>
    /// Tipo de operação: credit, debit, reserve, capture, reversal, transfer
    /// </summary>
    /// <example>credit</example>
    [JsonPropertyName("operation")]
    [Required(ErrorMessage = "Operation é obrigatório")]
    [Description("Tipo de operação")]
    public string Operation { get; init; } = null!;
    
    /// <summary>
    /// ID (GUID) da conta que receberá a transação
    /// </summary>
    /// <example>85d7a1d9-68b4-4e62-8957-556adbf8d996</example>
    [JsonPropertyName("account_id")]
    [Required(ErrorMessage = "Account ID é obrigatório")]
    [Description("ID da conta")]
    public string AccountId { get; init; } = null!;
    
    /// <summary>
    /// Valor da transação em centavos (100000 = R$ 1.000,00)
    /// </summary>
    /// <example>100000</example>
    [JsonPropertyName("amount")]
    [Required(ErrorMessage = "Amount é obrigatório")]
    [Range(1, long.MaxValue, ErrorMessage = "Amount deve ser maior que zero")]
    [Description("Valor em centavos")]
    public long Amount { get; init; }
    
    /// <summary>
    /// Código da moeda (BRL, USD, EUR)
    /// </summary>
    /// <example>BRL</example>
    [JsonPropertyName("currency")]
    [Required(ErrorMessage = "Currency é obrigatório")]
    [Description("Código da moeda")]
    public string Currency { get; init; } = null!;
    
    /// <summary>
    /// ID único de referência para idempotência (não pode repetir)
    /// </summary>
    /// <example>DEP-001-20260212123045</example>
    [JsonPropertyName("reference_id")]
    [Required(ErrorMessage = "Reference ID é obrigatório")]
    [Description("ID de referência único")]
    public string ReferenceId { get; init; } = null!;
    
    /// <summary>
    /// ID da conta destino (obrigatório para transfer)
    /// </summary>
    /// <example>13edef1a-ee42-49c6-be4d-7b0e4ae3ba15</example>
    [JsonPropertyName("target_account_id")]
    [Description("ID da conta destino (transfer)")]
    public string? TargetAccountId { get; init; }
    
    /// <summary>
    /// ID da transação original (obrigatório para capture e reversal)
    /// </summary>
    /// <example>bdc825f2-8975-401b-965f-9c122fcff91c</example>
    [JsonPropertyName("original_transaction_id")]
    [Description("ID da transação original (capture/reversal)")]
    public string? OriginalTransactionId { get; init; }
    
    /// <summary>
    /// Metadados adicionais da transação
    /// </summary>
    [JsonPropertyName("metadata")]
    [Description("Metadados adicionais")]
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Resposta do processamento de transação
/// </summary>
public record TransactionResponse
{
    /// <summary>
    /// ID único da transação processada
    /// </summary>
    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; init; } = null!;
    
    /// <summary>
    /// Status da transação (success, failed, pending)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = null!;
    
    /// <summary>
    /// Saldo da conta em centavos após a transação
    /// </summary>
    [JsonPropertyName("balance")]
    public long Balance { get; init; }
    
    /// <summary>
    /// Saldo reservado da conta em centavos
    /// </summary>
    [JsonPropertyName("reserved_balance")]
    public long ReservedBalance { get; init; }
    
    /// <summary>
    /// Saldo disponível da conta em centavos
    /// </summary>
    [JsonPropertyName("available_balance")]
    public long AvailableBalance { get; init; }
    
    /// <summary>
    /// Data e hora de processamento da transação (ISO 8601)
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = null!;
    
    /// <summary>
    /// Mensagem de erro (se houver)
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; init; }
}
