using Microsoft.AspNetCore.Identity;
using Vault.Domain.Entities;
using Vault.Service.DTOs;
using Vault.Service.Interfaces;
using Vault.Service.Security;

namespace Vault.Service.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IEncryptionService _encryptionService;


    public AuthenticationService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IEncryptionService encryptionService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _encryptionService = encryptionService;
    }
    
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            
            if (request.Password != request.ConfirmPassword)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Passwords do not match"
                };
            }
            
            var existingUser =
                await _userManager.FindByNameAsync(request.Username);
            
            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Username already exists"
                };
            }
            
            var existingEmail =
                await _userManager.FindByEmailAsync(request.Email);
            
            if (existingEmail != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email already exists"
                };
            }
            
            // Generate encryption salt for bank data
            var (_, salt) =
                _encryptionService.DeriveKeyFromPassword(
                    request.Password);
            
            var user = new User
            {
                UserName = request.Username,
                Email = request.Email,

                EncryptionKeyHash = salt,

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            var result =
                await _userManager.CreateAsync(
                    user,
                    request.Password);
            
            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = string.Join(
                        ", ",
                        result.Errors.Select(x => x.Description))
                };
            }
            
            return new AuthResponse
            {
                Success = true,
                Message = "User registered successfully",

                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.UserName!,
                    Email = user.Email!,
                    CreatedAt = user.CreatedAt
                }
            };
        }
        catch(Exception ex)
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

            var user = await _userManager.FindByNameAsync(request.Username);

            if(user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                request.Password,
                isPersistent: true,
                lockoutOnFailure: false);


            if(!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }


            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",

                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.UserName!,
                    Email = user.Email!,
                    CreatedAt = user.CreatedAt
                }
            };

        }
        catch(Exception ex)
        {
            return new AuthResponse
            {
                Success = false,
                Message = $"Login failed: {ex.Message}"
            };
        }
    }
    
    public async Task<User?> GetUserAsync(Guid userId)
    {
        return await _userManager.FindByIdAsync(
            userId.ToString());
    }
    
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        return await _userManager.FindByNameAsync(username);
    }
}