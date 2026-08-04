namespace Vault.Service.Security;

/// <summary>
/// Interface for encryption/decryption operations
/// </summary>
public interface IEncryptionService
{
    (string encryptedValue, string iv) Encrypt(string plainText, byte[] encryptionKey);
    string Decrypt(string encryptedText, byte[] encryptionKey, string iv);
    (byte[] key, string salt) DeriveKeyFromPassword(string password, string? salt = null);
}
