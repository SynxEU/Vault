using Vault.Service.DTOs;

namespace Vault.Service.Interfaces;

public interface IBankAccountService
{
    Task<ServiceResponse<BankAccountDetailDto>> CreateAccountAsync(int userId, CreateBankAccountRequest request);
    Task<ServiceResponse<BankAccountDetailDto>> GetAccountAsync(int userId, int accountId);
    Task<ServiceResponse<List<BankAccountDto>>> GetAllAccountsAsync(int userId);
    Task<ServiceResponse<BankAccountDetailDto>> UpdateAccountAsync(int userId, UpdateBankAccountRequest request);
    Task<ServiceResponse<bool>> DeleteAccountAsync(int userId, int accountId);
    
    Task<ServiceResponse<TransactionDto>> CreateTransactionAsync(int userId, CreateTransactionRequest request);
    Task<ServiceResponse<List<TransactionDto>>> GetAccountTransactionsAsync(int userId, int accountId);
    Task<ServiceResponse<bool>> DeleteTransactionAsync(int userId, int transactionId);
}
