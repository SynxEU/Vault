using BCrypt.Net;

namespace Vault.Service.Security;

/// <summary>
/// Service for password hashing using BCrypt
/// </summary>
public class HashingService : IHashingService
{
    /// <summary>
    /// Hashes a password using BCrypt with automatic salt generation
    /// </summary>
    /// <param name="password">The password to hash</param>
    /// <returns>The hashed password with embedded salt</returns>
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        // BCrypt.HashPassword automatically generates salt and embeds it in the hash
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verifies a password against a BCrypt hash
    /// </summary>
    /// <param name="password">The password to verify</param>
    /// <param name="hash">The BCrypt hash to verify against</param>
    /// <returns>True if password matches the hash, false otherwise</returns>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));
        
        if (string.IsNullOrEmpty(hash))
            throw new ArgumentException("Hash cannot be empty", nameof(hash));

        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
