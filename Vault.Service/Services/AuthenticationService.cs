using Microsoft.EntityFrameworkCore;
using Vault.Domain.Entities;
using Vault.Domain.Persistence;
using Vault.Service.DTOs;
using Vault.Service.Interfaces;
using Vault.Service.Security;

namespace Vault.Service.Services;

/// <summary>
/// Implementation of authentication service
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly VaultDbContext _dbContext;
    private readonly IHashingService _hashingService;
    private readonly IEncryptionService _encryptionService;

    public AuthenticationService(
        VaultDbContext dbContext,
        IHashingService hashingService,
        IEncryptionService encryptionService)
    {
        _dbContext = dbContext;
        _hashingService = hashingService;
        _encryptionService = encryptionService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            // Validation
            if (string.IsNullOrWhiteSpace(request.Username))
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = "Username is required" 
                };

            if (string.IsNullOrWhiteSpace(request.Email))
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = "Email is required" 
                };

            if (string.IsNullOrWhiteSpace(request.Password))
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = "Password is required" 
                };

            if (request.Password != request.ConfirmPassword)
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = "Passwords do not match" 
                };

            if (request.Password.Length < 8)
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = "Password must be at least 8 characters long" 
                };

            // Check if username or email already exists
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

            if (existingUser != null)
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = "Username or email already exists" 
                };

            // Hash password
            var passwordHash = _hashingService.HashPassword(request.Password);

            // Derive encryption key from password
            var (_, salt) = _encryptionService.DeriveKeyFromPassword(request.Password);

            // Create new user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                EncryptionKeyHash = salt, // Store the salt for later key derivation
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                Message = "User registered successfully",
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse
            {
                Success = false,
                Message = $"Registration failed: {ex.Message}"
            };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Username))
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = "Username is required" 
                };

            if (string.IsNullOrWhiteSpace(request.Password))
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = "Password is required" 
                };

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = "Invalid username or password" 
                };

            // Verify password
            var passwordValid = _hashingService.VerifyPassword(request.Password, user.PasswordHash);
            if (!passwordValid)
                return new AuthResponse 
                { 
                    Success = false, 
                    Message = "Invalid username or password" 
                };

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            return new AuthResponse
            {
                Success = false,
                Message = $"Login failed: {ex.Message}"
            };
        }
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        return await _dbContext.Users.FindAsync(userId);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
    }
}
