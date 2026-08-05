using Vault.Service.DTOs;

namespace Vault.Service.Interfaces;

public interface IBankAccountService
{
    Task<ServiceResponse<BankAccountDetailDto>> CreateAccountAsync(Guid userId, CreateBankAccountRequest request);
    Task<ServiceResponse<BankAccountDetailDto>> GetAccountAsync(Guid userId, Guid accountId);
    Task<ServiceResponse<List<BankAccountDto>>> GetAllAccountsAsync(Guid userId);
    Task<ServiceResponse<BankAccountDetailDto>> UpdateAccountAsync(Guid userId, UpdateBankAccountRequest request);
    Task<ServiceResponse<bool>> DeleteAccountAsync(Guid userId, Guid accountId);
}
