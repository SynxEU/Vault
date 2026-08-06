using System.ComponentModel.DataAnnotations;
using Vault.Domain.Entities.Enums;

namespace Vault.Service.DTOs;

/// <summary>
/// Transaction returned to client
/// </summary>
public class TransactionDto
{
    public Guid Id { get; set; }

    public DateTime TransactionDate { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }

    public TransactionStatus Status { get; set; }

    public string? RecipientName { get; set; }

    public string? RecipientRegistrationNumber { get; set; }
    
    public string? RecipientAccountNumber { get; set; }
}


/// <summary>
/// Create transaction request
/// </summary>
public class CreateTransactionRequest
{
    public Guid BankAccountId { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Description { get; set; } = string.Empty;
    
    [Range(typeof(decimal), "0,01", "9999999999999,99")]
    public decimal Amount { get; set; }
    
    [EnumDataType(typeof(TransactionType))]
    public TransactionType Type { get; set; }
    
    // External recipient information
    [StringLength(255)]
    public string? RecipientName { get; set; }

    [StringLength(10)]
    public string? RecipientRegistrationNumber { get; set; }
    
    [StringLength(50)]
    public string? RecipientAccountNumber { get; set; }
}

/// <summary>
/// Update transaction request
/// </summary>
public class UpdateTransactionRequest
{
    public Guid Id { get; set; }

    public string Description { get; set; } = "";

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }

    public string? RecipientName { get; set; }

    public string? RecipientRegistrationNumber { get; set; }

    public string? RecipientAccountNumber { get; set; }
}