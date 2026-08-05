using System.ComponentModel.DataAnnotations;
using Vault.Domain.Entities.Enums;

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

/// <summary>
/// Used for account lists
/// </summary>
public class BankAccountDto
{
    public Guid Id { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public AccountType AccountType { get; set; }

    public string RegistrationNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Full decrypted account information
/// </summary>
public class BankAccountDetailDto
{
    public Guid Id { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public AccountType AccountType { get; set; }

    public string RegistrationNumber { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public string IBAN { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Create new bank account request
/// </summary>
public class CreateBankAccountRequest
{
    [Required]
    [StringLength(255)]
    public string AccountName { get; set; } = string.Empty;
    
    [EnumDataType(typeof(AccountType))]
    public AccountType AccountType { get; set; }

    [StringLength(10)]
    public string RegistrationNumber { get; set; } = string.Empty;

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

/// <summary>
/// Update existing bank account request
/// </summary>
public class UpdateBankAccountRequest
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(255)]
    public string AccountName { get; set; } = string.Empty;
    
    [EnumDataType(typeof(AccountType))]
    public AccountType AccountType { get; set; }
    
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Balance { get; set; }
}