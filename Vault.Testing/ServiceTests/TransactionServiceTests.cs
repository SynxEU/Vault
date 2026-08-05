using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vault.Domain.Entities;
using Vault.Domain.Entities.Enums;
using Vault.Domain.Persistence;
using Vault.Service.DTOs;
using Vault.Service.Security;
using Vault.Service.Services;

namespace Vault.Testing.ServiceTests;

public class TransactionServiceTests
{
    [Fact]
    public async Task CreateTransaction_Deposit_ReturnsSuccess()
    {
        // Arrange
        using var dbContext = CreateDbContext();

        var encryption = new EncryptionService();

        var (_, salt) =
            encryption.DeriveKeyFromPassword(
                "StrongPass123!");

        var user =
            new User
            {
                Id = Guid.NewGuid(),
                UserName = "user1",
                Email = "user@test.com",
                EncryptionKeyHash = salt,
                PasswordHash =
                    new PasswordHasher<User>()
                        .HashPassword(
                            null!,
                            "StrongPass123!")
            };

        dbContext.Users.Add(user);

        var (key, _) =
            encryption.DeriveKeyFromPassword(
                user.EncryptionKeyHash,
                user.EncryptionKeyHash);

        var (balance, balanceIv) =
            encryption.Encrypt("1000", key);

        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AccountName = "Main",
            AccountType = AccountType.Personal,
            EncryptedRegistrationNumber = Encrypt("1234", encryption, key),
            EncryptedAccountNumber = Encrypt("1234567890", encryption, key),
            EncryptedIBAN = Encrypt("DK111111111111111111", encryption, key),
            EncryptedBalance = $"{balance}:{balanceIv}",
            EncryptedCurrency = Encrypt("DKK", encryption, key),
            EncryptedBankName = Encrypt("Test Bank", encryption, key)
        };

        dbContext.BankAccounts.Add(account);

        await dbContext.SaveChangesAsync();

        var service =
            new TransactionService(
                dbContext,
                encryption);

        var request = new CreateTransactionRequest
        {
            BankAccountId = account.Id,
            Description = "Salary",
            Amount = 500,
            Type = TransactionType.Deposit
        };

        // Act

        var result =
            await service.CreateTransactionAsync(
                user.Id,
                request);

        // Assert

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(500m, result.Data.Amount);
        Assert.Equal(TransactionType.Deposit, result.Data.Type);
        Assert.Equal(TransactionStatus.Completed, result.Data.Status);
    }

    [Fact]
    public async Task CreateTransaction_InsufficientFunds_ReturnsFailure()
    {
        using var dbContext = CreateDbContext();

        var encryption = new EncryptionService();

        var (_, salt) =
            encryption.DeriveKeyFromPassword(
                "StrongPass123!");

        var user =
            new User
            {
                Id = Guid.NewGuid(),
                UserName = "user1",
                Email = "user@test.com",
                EncryptionKeyHash = salt,
                PasswordHash =
                    new PasswordHasher<User>()
                        .HashPassword(
                            null!,
                            "StrongPass123!")
            };

        dbContext.Users.Add(user);

        var (key, _) =
            encryption.DeriveKeyFromPassword(
                user.EncryptionKeyHash,
                user.EncryptionKeyHash);

        var (balance, balanceIv) =
            encryption.Encrypt("100", key);

        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AccountName = "Main",
            AccountType = AccountType.Personal,
            EncryptedRegistrationNumber = Encrypt("1234", encryption, key),
            EncryptedAccountNumber = Encrypt("1234567890", encryption, key),
            EncryptedIBAN = Encrypt("DK111111111111111111", encryption, key),
            EncryptedBalance = $"{balance}:{balanceIv}",
            EncryptedCurrency = Encrypt("DKK", encryption, key),
            EncryptedBankName = Encrypt("Test Bank", encryption, key)
        };

        dbContext.BankAccounts.Add(account);

        await dbContext.SaveChangesAsync();

        var service =
            new TransactionService(
                dbContext,
                encryption);

        var result =
            await service.CreateTransactionAsync(
                user.Id,
                new CreateTransactionRequest
                {
                    BankAccountId = account.Id,
                    Description = "Purchase",
                    Amount = 500,
                    Type = TransactionType.Purchase
                });

        Assert.False(result.Success);
        Assert.Equal("Insufficient funds", result.Message);
    }

    [Fact]
    public async Task CreateTransaction_InvalidAccount_ReturnsFailure()
    {
        using var dbContext = CreateDbContext();

        var encryption = new EncryptionService();

        var (_, salt) =
            encryption.DeriveKeyFromPassword(
                "StrongPass123!");

        var user =
            new User
            {
                Id = Guid.NewGuid(),
                UserName = "user1",
                Email = "user@test.com",
                EncryptionKeyHash = salt,
                PasswordHash =
                    new PasswordHasher<User>()
                        .HashPassword(
                            null!,
                            "StrongPass123!")
            };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service =
            new TransactionService(
                dbContext,
                encryption);

        var result =
            await service.CreateTransactionAsync(
                user.Id,
                new CreateTransactionRequest
                {
                    BankAccountId = Guid.NewGuid(),
                    Description = "Test",
                    Amount = 100,
                    Type = TransactionType.Deposit
                });

        Assert.False(result.Success);
        Assert.Equal("Account not found", result.Message);
    }

    [Fact]
    public async Task GetTransactions_ReturnsCreatedTransaction()
    {
        using var dbContext = CreateDbContext();

        var encryption = new EncryptionService();

        var (_, salt) =
            encryption.DeriveKeyFromPassword(
                "StrongPass123!");

        var user =
            new User
            {
                Id = Guid.NewGuid(),
                UserName = "user1",
                Email = "user@test.com",
                EncryptionKeyHash = salt,
                PasswordHash =
                    new PasswordHasher<User>()
                        .HashPassword(
                            null!,
                            "StrongPass123!")
            };

        dbContext.Users.Add(user);

        var (key, _) =
            encryption.DeriveKeyFromPassword(
                user.EncryptionKeyHash,
                user.EncryptionKeyHash);

        var (balance, balanceIv) =
            encryption.Encrypt("1000", key);

        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AccountName = "Main",
            AccountType = AccountType.Personal,
            EncryptedRegistrationNumber = Encrypt("1234", encryption, key),
            EncryptedAccountNumber = Encrypt("1234567890", encryption, key),
            EncryptedIBAN = Encrypt("DK111111111111111111", encryption, key),
            EncryptedBalance = $"{balance}:{balanceIv}",
            EncryptedCurrency = Encrypt("DKK", encryption, key),
            EncryptedBankName = Encrypt("Test Bank", encryption, key)
        };

        dbContext.BankAccounts.Add(account);
        await dbContext.SaveChangesAsync();

        var service =
            new TransactionService(
                dbContext,
                encryption);

        await service.CreateTransactionAsync(
            user.Id,
            new CreateTransactionRequest
            {
                BankAccountId = account.Id,
                Description = "Deposit",
                Amount = 500,
                Type = TransactionType.Deposit
            });

        var result =
            await service.GetAccountTransactionsAsync(
                user.Id,
                account.Id);

        Assert.True(result.Success);
        Assert.Single(result.Data!);
        Assert.Equal("Deposit", result.Data![0].Description);
    }

    [Fact]
    public async Task DeleteTransaction_RemovesTransaction()
    {
        using var dbContext = CreateDbContext();

        var encryption = new EncryptionService();

        var (_, salt) =
            encryption.DeriveKeyFromPassword(
                "StrongPass123!");

        var user =
            new User
            {
                Id = Guid.NewGuid(),
                UserName = "user1",
                Email = "user@test.com",
                EncryptionKeyHash = salt,
                PasswordHash =
                    new PasswordHasher<User>()
                        .HashPassword(
                            null!,
                            "StrongPass123!")
            };

        dbContext.Users.Add(user);

        var (key, _) =
            encryption.DeriveKeyFromPassword(
                user.EncryptionKeyHash,
                user.EncryptionKeyHash);

        var (balance, balanceIv) =
            encryption.Encrypt("1000", key);

        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AccountName = "Main",
            AccountType = AccountType.Personal,
            EncryptedRegistrationNumber = Encrypt("1234", encryption, key),
            EncryptedAccountNumber = Encrypt("1234567890", encryption, key),
            EncryptedIBAN = Encrypt("DK111111111111111111", encryption, key),
            EncryptedBalance = $"{balance}:{balanceIv}",
            EncryptedCurrency = Encrypt("DKK", encryption, key),
            EncryptedBankName = Encrypt("Test Bank", encryption, key)
        };

        dbContext.BankAccounts.Add(account);
        await dbContext.SaveChangesAsync();

        var service =
            new TransactionService(
                dbContext,
                encryption);

        var created =
            await service.CreateTransactionAsync(
                user.Id,
                new CreateTransactionRequest
                {
                    BankAccountId = account.Id,
                    Description = "Deposit",
                    Amount = 100,
                    Type = TransactionType.Deposit
                });

        var deleted =
            await service.DeleteTransactionAsync(
                user.Id,
                created.Data!.Id);

        Assert.True(deleted.Success);
        Assert.Empty(dbContext.Transactions);
    }

    private static string Encrypt(
        string value,
        EncryptionService encryption,
        byte[] key)
    {
        var (cipher, iv) =
            encryption.Encrypt(value, key);

        return $"{cipher}:{iv}";
    }

    private static VaultDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<VaultDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        return new VaultDbContext(options);
    }
}