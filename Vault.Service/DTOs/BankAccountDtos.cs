using System.ComponentModel.DataAnnotations;

namespace Vault.Service.DTOs;

/// <summary>
/// Generic response wrapper for API operations
/// </summary>
public class ServiceResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public class BankAccountDto
{
    public int Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class BankAccountDetailDto
{
    public int Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateBankAccountRequest
{
    [Required]
    [StringLength(255)]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string IBAN { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Balance { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "DKK";

    [Required]
    [StringLength(255)]
    public string BankName { get; set; } = string.Empty;
}

public class UpdateBankAccountRequest
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string AccountName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Balance { get; set; }
}

public class TransactionDto
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Recipient { get; set; }
}

public class CreateTransactionRequest
{
    public int BankAccountId { get; set; }

    [Required]
    [StringLength(255)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [Required]
    [RegularExpression("^(Debit|Credit)$", ErrorMessage = "Type must be Debit or Credit")]
    public string Type { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Recipient { get; set; }
}
