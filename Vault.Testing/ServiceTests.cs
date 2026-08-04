using Microsoft.EntityFrameworkCore;
using Moq;
using Vault.Domain.Entities;
using Vault.Domain.Persistence;
using Vault.Service.DTOs;
using Vault.Service.Security;
using Vault.Service.Services;

namespace Vault.Testing;

public class HashingServiceTests
{
    private readonly HashingService _service = new();

    [Fact]
    public void HashAndVerifyPassword_RoundTrips()
    {
        var hash = _service.HashPassword("StrongPass123!");

        Assert.True(_service.VerifyPassword("StrongPass123!", hash));
        Assert.False(_service.VerifyPassword("WrongPass123!", hash));
    }
}

public class EncryptionServiceTests
{
    private readonly EncryptionService _service = new();

    [Fact]
    public void EncryptAndDecrypt_ReturnsOriginalValue()
    {
        var (key, _) = _service.DeriveKeyFromPassword("StrongPass123!");

        var (encrypted, iv) = _service.Encrypt("Sensitive data", key);
        var decrypted = _service.Decrypt(encrypted, key, iv);

        Assert.Equal("Sensitive data", decrypted);
    }

    [Fact]
    public void DeriveKeyFromPassword_WithSalt_ReturnsSameKey()
    {
        var (key1, salt) = _service.DeriveKeyFromPassword("StrongPass123!");
        var (key2, salt2) = _service.DeriveKeyFromPassword("StrongPass123!", salt);

        Assert.Equal(salt, salt2);
        Assert.Equal(Convert.ToBase64String(key1), Convert.ToBase64String(key2));
    }
}

public class AuthenticationServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUserAndStoresSalt()
    {
        using var dbContext = CreateDbContext();
        var hashing = new Mock<IHashingService>();
        var encryption = new Mock<IEncryptionService>();

        hashing.Setup(s => s.HashPassword("StrongPass123!"))
            .Returns("hashed-password");
        encryption.Setup(s => s.DeriveKeyFromPassword("StrongPass123!", null))
            .Returns((new byte[32], "salt-value"));

        var service = new AuthenticationService(dbContext, hashing.Object, encryption.Object);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Username = "user1",
            Email = "user1@example.com",
            Password = "StrongPass123!",
            ConfirmPassword = "StrongPass123!"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal("salt-value", await dbContext.Users.Select(u => u.EncryptionKeyHash).SingleAsync());
    }

    [Fact]
    public async Task LoginAsync_ReturnsSuccessForValidPassword()
    {
        using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Username = "user1",
            Email = "user1@example.com",
            PasswordHash = "hashed-password",
            EncryptionKeyHash = "salt-value"
        });
        await dbContext.SaveChangesAsync();

        var hashing = new Mock<IHashingService>();
        hashing.Setup(s => s.VerifyPassword("StrongPass123!", "hashed-password"))
            .Returns(true);

        var service = new AuthenticationService(dbContext, hashing.Object, new EncryptionService());

        var result = await service.LoginAsync(new LoginRequest
        {
            Username = "user1",
            Password = "StrongPass123!"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.User);
    }

    private static VaultDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VaultDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new VaultDbContext(options);
    }
}

public class BankAccountServiceTests
{
    [Fact]
    public async Task CreateAndReadAccount_ReturnsDecryptedValues()
    {
        using var dbContext = CreateDbContext();
        var encryption = new EncryptionService();
        var (_, salt) = encryption.DeriveKeyFromPassword("StrongPass123!");

        dbContext.Users.Add(new User
        {
            Id = 1,
            Username = "user1",
            Email = "user1@example.com",
            PasswordHash = "hashed-password",
            EncryptionKeyHash = salt
        });
        await dbContext.SaveChangesAsync();

        var service = new BankAccountService(dbContext, encryption);
        var createResult = await service.CreateAccountAsync(1, new CreateBankAccountRequest
        {
            AccountName = "Main account",
            AccountNumber = "1234567890",
            IBAN = "DK5000400440116243",
            Balance = 5000.50m,
            Currency = "DKK",
            BankName = "Test Bank"
        });

        Assert.True(createResult.Success);

        var readResult = await service.GetAccountAsync(1, createResult.Data!.Id);

        Assert.True(readResult.Success);
        Assert.Equal("Main account", readResult.Data!.AccountName);
        Assert.Equal("1234567890", readResult.Data.AccountNumber);
        Assert.Equal(5000.50m, readResult.Data.Balance);
    }

    [Fact]
    public async Task CreateTransactionAsync_AllowsMissingRecipient()
    {
        using var dbContext = CreateDbContext();
        var encryption = new EncryptionService();
        var (_, salt) = encryption.DeriveKeyFromPassword("StrongPass123!");

        dbContext.Users.Add(new User
        {
            Id = 1,
            Username = "user1",
            Email = "user1@example.com",
            PasswordHash = "hashed-password",
            EncryptionKeyHash = salt
        });
        dbContext.BankAccounts.Add(new BankAccount
        {
            Id = 10,
            UserId = 1,
            AccountName = "Main account",
            EncryptedAccountNumber = "x:y",
            EncryptedIBAN = "x:y",
            EncryptedBalance = "x:y",
            EncryptedCurrency = "x:y",
            EncryptedBankName = "x:y"
        });
        await dbContext.SaveChangesAsync();

        var service = new BankAccountService(dbContext, encryption);

        var createResult = await service.CreateTransactionAsync(1, new CreateTransactionRequest
        {
            BankAccountId = 10,
            Description = "Coffee",
            Amount = 12.50m,
            Type = "Debit",
            Recipient = null
        });

        Assert.True(createResult.Success);
        Assert.Null(createResult.Data!.Recipient);
    }

    private static VaultDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VaultDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new VaultDbContext(options);
    }
}
