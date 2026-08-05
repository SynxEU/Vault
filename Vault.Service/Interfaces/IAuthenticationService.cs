using Vault.Domain.Entities;
using Vault.Service.DTOs;

namespace Vault.Service.Interfaces;

/// <summary>
/// Interface for authentication operations
/// </summary>
public interface IAuthenticationService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);

    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    Task<User?> GetUserAsync(Guid userId);

    Task<User?> GetUserByUsernameAsync(string username);
}
