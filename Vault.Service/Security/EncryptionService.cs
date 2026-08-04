using System.Security.Cryptography;
using System.Text;

namespace Vault.Service.Security;

/// <summary>
/// Service for AES encryption and decryption of vault secrets
/// </summary>
public class EncryptionService : IEncryptionService
{
    /// <summary>
    /// Encrypts a value using AES encryption
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <param name="encryptionKey">The encryption key (must be 32 bytes for AES-256)</param>
    /// <returns>Tuple of (encryptedValue, iv)</returns>
    public (string encryptedValue, string iv) Encrypt(string plainText, byte[] encryptionKey)
    {
        if (plainText is null)
            throw new ArgumentNullException(nameof(plainText));
        
        if (encryptionKey.Length != 32)
            throw new ArgumentException("Encryption key must be 32 bytes for AES-256", nameof(encryptionKey));

        using (var aes = Aes.Create())
        {
            aes.Key = encryptionKey;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            {
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        var encryptedData = ms.ToArray();
                        var encryptedBase64 = Convert.ToBase64String(encryptedData);
                        var ivBase64 = Convert.ToBase64String(aes.IV);
                        
                        return (encryptedBase64, ivBase64);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Decrypts a value using AES encryption
    /// </summary>
    /// <param name="encryptedText">The encrypted text (base64 encoded)</param>
    /// <param name="encryptionKey">The encryption key (must be 32 bytes for AES-256)</param>
    /// <param name="iv">The initialization vector (base64 encoded)</param>
    /// <returns>The decrypted plain text</returns>
    public string Decrypt(string encryptedText, byte[] encryptionKey, string iv)
    {
        if (string.IsNullOrEmpty(encryptedText))
            throw new ArgumentException("Encrypted text cannot be empty", nameof(encryptedText));
        
        if (encryptionKey.Length != 32)
            throw new ArgumentException("Encryption key must be 32 bytes for AES-256", nameof(encryptionKey));
        
        if (string.IsNullOrEmpty(iv))
            throw new ArgumentException("IV cannot be empty", nameof(iv));

        try
        {
            var encryptedData = Convert.FromBase64String(encryptedText);
            var ivBytes = Convert.FromBase64String(iv);

            using (var aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    using (var ms = new MemoryStream(encryptedData))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Decryption failed. The key or IV may be incorrect.", ex);
        }
    }

    /// <summary>
    /// Derives a 32-byte encryption key from a password using PBKDF2
    /// </summary>
    /// <param name="password">The password</param>
    /// <param name="salt">The salt (if null, a new salt will be generated)</param>
    /// <returns>Tuple of (key, salt in base64)</returns>
    public (byte[] key, string salt) DeriveKeyFromPassword(string password, string? salt = null)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        byte[] saltBytes;
        
        if (string.IsNullOrEmpty(salt))
        {
            // Generate new salt
            saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
        }
        else
        {
            saltBytes = Convert.FromBase64String(salt);
        }

        using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256))
        {
            var key = pbkdf2.GetBytes(32); // 256 bits for AES-256
            var saltBase64 = Convert.ToBase64String(saltBytes);
            return (key, saltBase64);
        }
    }
}
