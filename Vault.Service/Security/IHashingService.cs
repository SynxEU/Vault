namespace Vault.Service.Security;

/// <summary>
/// Interface for password hashing operations
/// </summary>
public interface IHashingService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
