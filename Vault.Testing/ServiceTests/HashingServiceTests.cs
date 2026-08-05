using Vault.Service.Security;

namespace Vault.Testing.ServiceTests;

public class HashingServiceTests
{
    private readonly HashingService _service = new();
    
    [Fact]
    public void HashAndVerifyPassword_RoundTrips()
    {
        // Arrange
        var password = "StrongPass123!";
        
        // Act
        var hash = _service.HashPassword(password);
        
        // Assert
        Assert.True(_service.VerifyPassword(password, hash));
        Assert.False(_service.VerifyPassword("WrongPassword", hash));
    }
}