using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vault.Domain.Entities;
using Vault.Domain.Persistence;
using Vault.Service.DTOs;
using Vault.Service.Security;
using Vault.Service.Services;

namespace Vault.Testing.ServiceTests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesIdentityUser()
    {
        // Arrange
        using var dbContext = CreateDbContext();

        var userManager = CreateUserManager();

        var signInManager = CreateSignInManager(
            userManager.Object);

        var encryption = new EncryptionService();

        var service = new AuthenticationService(
            userManager.Object,
            signInManager.Object,
            encryption);

        var request = new RegisterRequest
        {
            Username = "user1",
            Email = "user1@test.com",
            Password = "StrongPass123!",
            ConfirmPassword = "StrongPass123!"
        };


        userManager
            .Setup(x => x.CreateAsync(
                It.IsAny<User>(),
                request.Password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<User, string>((user, password) =>
            {
                user.PasswordHash =
                    new PasswordHasher<User>()
                        .HashPassword(user, password);

                dbContext.Users.Add(user);
                dbContext.SaveChanges();
            });


        // Act
        var result =
            await service.RegisterAsync(request);


        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.User);

        var user =
            await dbContext.Users.FirstAsync();

        Assert.Equal(
            "user1",
            user.UserName);

        Assert.False(
            string.IsNullOrEmpty(user.EncryptionKeyHash));
    }


    [Fact]
    public async Task LoginAsync_WithCorrectPassword_ReturnsSuccess()
    {
        // Arrange
        using var dbContext = CreateDbContext();

        var user =
            new User
            {
                Id = Guid.NewGuid(),
                UserName = "user1",
                Email = "user@test.com",
                PasswordHash =
                    new PasswordHasher<User>()
                        .HashPassword(
                            null!,
                            "StrongPass123!")
            };


        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();


        var userManager = CreateUserManager();

        userManager
            .Setup(x =>
                x.FindByNameAsync("user1"))
            .ReturnsAsync(user);


        var signInManager =
            CreateSignInManager(
                userManager.Object);


        signInManager
            .Setup(x =>
                x.PasswordSignInAsync(
                    user,
                    "StrongPass123!",
                    true,
                    false))
            .ReturnsAsync(SignInResult.Success);


        var service =
            new AuthenticationService(
                userManager.Object,
                signInManager.Object,
                new EncryptionService());


        var request =
            new LoginRequest
            {
                Username = "user1",
                Password = "StrongPass123!"
            };


        // Act
        var result =
            await service.LoginAsync(request);


        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.User);
    }
    
    private static Mock<SignInManager<User>> CreateSignInManager(
        UserManager<User> userManager)
    {
        var contextAccessor =
            new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();

        var claimsFactory =
            new Mock<IUserClaimsPrincipalFactory<User>>();

        return new Mock<SignInManager<User>>(
            userManager,
            contextAccessor.Object,
            claimsFactory.Object,
            null!,
            null!,
            null!,
            null!);
    }
    
    private static Mock<UserManager<User>> CreateUserManager()
    {
        var store =
            new Mock<IUserStore<User>>();

        return new Mock<UserManager<User>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
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