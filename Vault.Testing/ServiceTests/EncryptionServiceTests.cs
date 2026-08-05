using Vault.Service.Security;

namespace Vault.Testing.ServiceTests;

public class EncryptionServiceTests
{
    private readonly EncryptionService _service = new();
    [Fact]
    public void EncryptAndDecrypt_ReturnsOriginalValue()
    {
        // Arrange
        var (key, _) =
            _service.DeriveKeyFromPassword("StrongPass123!");
        var originalText = "Sensitive data";
        
        // Act
        var (encrypted, iv) =
            _service.Encrypt(originalText, key);
        var decrypted =
            _service.Decrypt(encrypted, key, iv);
        
        // Assert
        Assert.Equal(originalText, decrypted);
    }
    
    [Fact]
    public void DeriveKeyFromPassword_WithSalt_ReturnsSameKey()
    {
        // Arrange
        var password = "StrongPass123!";

        // Act
        var (key1, salt) =
            _service.DeriveKeyFromPassword(password);
        var (key2, salt2) =
            _service.DeriveKeyFromPassword(password, salt);
        
        // Assert
        Assert.Equal(salt, salt2);
        Assert.Equal(
            Convert.ToBase64String(key1),
            Convert.ToBase64String(key2));
    }
}