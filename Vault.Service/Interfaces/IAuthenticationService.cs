using Vault.Domain.Entities;
using Vault.Service.DTOs;

namespace Vault.Service.Interfaces;

/// <summary>
/// Interface for authentication operations
/// </summary>
public interface IAuthenticationService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<User?> GetUserAsync(int userId);
    Task<User?> GetUserByUsernameAsync(string username);
}
