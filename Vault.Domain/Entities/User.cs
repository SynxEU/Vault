using Microsoft.AspNetCore.Identity;

namespace Vault.Domain.Entities;

/// <summary>
/// Represents a user in the Vault system
/// </summary>
public class User : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Salt used for deriving the user's encryption key
    /// </summary>
    public string EncryptionKeyHash { get; set; } = string.Empty;
    
    public ICollection<BankAccount> BankAccounts { get; set; }
        = new List<BankAccount>();
}