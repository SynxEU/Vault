using Vault.Service.DTOs;

namespace Vault.Service.Interfaces;

public interface ITransactionService
{
    Task<ServiceResponse<TransactionDto>> CreateTransactionAsync(
        Guid userId,
        CreateTransactionRequest request);

    Task<ServiceResponse<List<TransactionDto>>> GetAccountTransactionsAsync(Guid userId, Guid accountId);
    Task<ServiceResponse<bool>> DeleteTransactionAsync(Guid userId, Guid transactionId);
    Task<ServiceResponse<TransactionDto>> UpdateTransactionAsync(
        Guid userId,
        UpdateTransactionRequest request);
}