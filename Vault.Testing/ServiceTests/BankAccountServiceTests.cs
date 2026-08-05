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

public class BankAccountServiceTests
{
    [Fact]
    public async Task GetAllAccounts_ReturnsOnlyUsersAccounts()
    {
        using var dbContext = CreateDbContext();

        var encryption = new EncryptionService();
        var (_, salt) = encryption.DeriveKeyFromPassword("Password123!");

        var user1 =
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

        var user2 =
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

        dbContext.Users.AddRange(user1, user2);
        await dbContext.SaveChangesAsync();

        var service = new BankAccountService(dbContext, encryption);

        for (int i = 0; i < 2; i++)
        {
            await service.CreateAccountAsync(user1.Id, new CreateBankAccountRequest
            {
                AccountName = $"Account {i}",
                AccountType = AccountType.Personal,
                RegistrationNumber = "1234",
                AccountNumber = $"11111{i}",
                IBAN = $"DK{i}",
                Balance = 100,
                Currency = "DKK",
                BankName = "Bank"
            });
        }

        await service.CreateAccountAsync(user2.Id, new CreateBankAccountRequest
        {
            AccountName = "Other",
            AccountType = AccountType.Personal,
            RegistrationNumber = "9999",
            AccountNumber = "999999",
            IBAN = "DK999",
            Balance = 200,
            Currency = "DKK",
            BankName = "Bank"
        });

        var result = await service.GetAllAccountsAsync(user1.Id);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
    }
    
    [Fact]
    public async Task UpdateAccount_UpdatesAllowedFields()
    {
        using var dbContext = CreateDbContext();

        var encryption = new EncryptionService();
        var (_, salt) = encryption.DeriveKeyFromPassword("Password123!");

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

        var service = new BankAccountService(dbContext, encryption);

        var created = await service.CreateAccountAsync(user.Id, new CreateBankAccountRequest
        {
            AccountName = "Old",
            AccountType = AccountType.Personal,
            RegistrationNumber = "1234",
            AccountNumber = "111",
            IBAN = "DK111",
            Balance = 100,
            Currency = "DKK",
            BankName = "Bank"
        });

        var update = await service.UpdateAccountAsync(user.Id,
            new UpdateBankAccountRequest
            {
                Id = created.Data!.Id,
                AccountName = "Updated",
                AccountType = AccountType.Business,
                Balance = 999
            });

        Assert.True(update.Success);
        Assert.Equal("Updated", update.Data!.AccountName);
        Assert.Equal(AccountType.Business, update.Data.AccountType);
        Assert.Equal(999m, update.Data.Balance);

        // These should NOT change
        Assert.Equal("1234", update.Data.RegistrationNumber);
        Assert.Equal("111", update.Data.AccountNumber);
        Assert.Equal("DK111", update.Data.IBAN);
        Assert.Equal("DKK", update.Data.Currency);
        Assert.Equal("Bank", update.Data.BankName);
    }
    
    [Fact]
    public async Task DeleteAccount_RemovesAccount()
    {
        using var dbContext = CreateDbContext();

        var encryption = new EncryptionService();
        var (_, salt) = encryption.DeriveKeyFromPassword("Password123!");

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

        var service = new BankAccountService(dbContext, encryption);

        var created = await service.CreateAccountAsync(user.Id,
            new CreateBankAccountRequest
            {
                AccountName = "Delete",
                AccountType = AccountType.Personal,
                RegistrationNumber = "1234",
                AccountNumber = "111",
                IBAN = "DK111",
                Balance = 50,
                Currency = "DKK",
                BankName = "Bank"
            });

        var delete = await service.DeleteAccountAsync(user.Id, created.Data!.Id);

        Assert.True(delete.Success);

        var read = await service.GetAccountAsync(user.Id, created.Data.Id);

        Assert.False(read.Success);
    }
    
    [Fact]
    public async Task CreateAccount_FailsWhenUserDoesNotExist()
    {
        using var dbContext = CreateDbContext();

        var service = new BankAccountService(dbContext, new EncryptionService());

        var result = await service.CreateAccountAsync(Guid.NewGuid(), new CreateBankAccountRequest());

        Assert.False(result.Success);
    }
    
    [Fact]
    public async Task GetAccount_ReturnsFailure_WhenAccountDoesNotExist()
    {
        using var dbContext = CreateDbContext();

        var encryption = new EncryptionService();
        var (_, salt) = encryption.DeriveKeyFromPassword("Password123!");

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

        var service = new BankAccountService(dbContext, encryption);

        var result = await service.GetAccountAsync(user.Id, Guid.NewGuid());

        Assert.False(result.Success);
    }
    
    private static VaultDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<VaultDbContext>()
            .UseInMemoryDatabase(
                Guid.NewGuid().ToString())
            .Options;
        return new VaultDbContext(options);
    }
}