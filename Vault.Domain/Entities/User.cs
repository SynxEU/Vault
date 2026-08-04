namespace Vault.Domain.Entities;

/// <summary>
/// Represents a user in the Vault system
/// </summary>
public class User
{
    public int Id { get; set; }
    
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// BCrypt hashed password with salt
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Salt used for deriving the user's encryption key
    /// </summary>
    public string EncryptionKeyHash { get; set; } = string.Empty;
    
    public ICollection<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
}
