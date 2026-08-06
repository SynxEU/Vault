using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vault.Domain.Entities;
using Vault.Domain.Entities.Enums;
using Vault.Domain.Persistence;
using Vault.Service.DTOs;
using Vault.Service.Interfaces;
using Vault.Service.Security;

namespace Vault.Service.Services;

public class TransactionService : ITransactionService
{
    private readonly VaultDbContext _dbContext;
    private readonly IEncryptionService _encryptionService;
    private const int MaxAccountsPerUser = 5;

    public TransactionService(
        VaultDbContext dbContext,
        IEncryptionService encryptionService)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
    }
    
    public async Task<ServiceResponse<TransactionDto>> CreateTransactionAsync(
        Guid userId,
        CreateTransactionRequest request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var account = await _dbContext.BankAccounts
                .FirstOrDefaultAsync(a =>
                    a.Id == request.BankAccountId &&
                    a.UserId == userId);

            if (account == null)
            {
                return new ServiceResponse<TransactionDto>
                {
                    Success = false,
                    Message = "Account not found"
                };
            }

            var user = await _dbContext.Users.FindAsync(userId);

            if (user == null)
            {
                return new ServiceResponse<TransactionDto>
                {
                    Success = false,
                    Message = "User not found"
                };
            }
            
            var (encryptionKey, _) =
                _encryptionService.DeriveKeyFromPassword(
                    user.EncryptionKeyHash,
                    user.EncryptionKeyHash);
            
            // Get current balance
            var (encryptedBalance, balanceIV) =
                ExtractEncryptedValue(account.EncryptedBalance);

            var currentBalance =
                decimal.Parse(
                    _encryptionService.Decrypt(
                        encryptedBalance,
                        encryptionKey,
                        balanceIV),
                    CultureInfo.InvariantCulture);
            
            // Calculate transaction effect
            var balanceChange =
                CalculateBalanceChange(
                    request.Type,
                    request.Amount);
            
            if (currentBalance + balanceChange < 0)
            {
                return new ServiceResponse<TransactionDto>
                {
                    Success = false,
                    Message = "Insufficient funds"
                };
            }
            
            var newBalance =
                currentBalance + balanceChange;
            
            // Update balance
            var (newEncryptedBalance, newBalanceIV) =
                _encryptionService.Encrypt(
                    newBalance.ToString(
                        CultureInfo.InvariantCulture),
                    encryptionKey);

            account.EncryptedBalance =
                $"{newEncryptedBalance}:{newBalanceIV}";
            
            // Encrypt description
            var (encryptedDescription, descIV) =
                _encryptionService.Encrypt(
                    request.Description,
                    encryptionKey);
            
            // Encrypt amount
            var (encryptedAmount, amountIV) =
                _encryptionService.Encrypt(
                    request.Amount.ToString(
                        CultureInfo.InvariantCulture),
                    encryptionKey);

            string? encryptedRecipientName = string.Empty;
            string? encryptedRecipientRegistration = string.Empty;
            string? encryptedRecipientAccount = string.Empty;

            if (request.Type == TransactionType.Transfer)
            {
                var recipientName =
                    request.RecipientName?.Trim() ?? string.Empty;

                var recipientRegistration =
                    request.RecipientRegistrationNumber?.Trim() ?? string.Empty;

                var recipientAccount =
                    request.RecipientAccountNumber?.Trim() ?? string.Empty;

                var (name, nameIV) =
                    _encryptionService.Encrypt(recipientName, encryptionKey);

                var (registration, registrationIV) =
                    _encryptionService.Encrypt(recipientRegistration, encryptionKey);

                var (accountNumber, accountIV) =
                    _encryptionService.Encrypt(recipientAccount, encryptionKey);

                encryptedRecipientName = $"{name}:{nameIV}";
                encryptedRecipientRegistration = $"{registration}:{registrationIV}";
                encryptedRecipientAccount = $"{accountNumber}:{accountIV}";
            }
            
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                BankAccountId =
                    request.BankAccountId,
                TransactionDate =
                    DateTime.UtcNow,
                Status =
                    TransactionStatus.Completed,
                Type =
                    request.Type,
                EncryptedDescription =
                    $"{encryptedDescription}:{descIV}",
                EncryptedAmount =
                    $"{encryptedAmount}:{amountIV}",
                EncryptedRecipientName = encryptedRecipientName,
                EncryptedRecipientRegistrationNumber = encryptedRecipientRegistration,
                EncryptedRecipientAccountNumber = encryptedRecipientAccount
            };

            _dbContext.BankAccounts.Update(account);
            
            _dbContext.Transactions.Add(transaction);
            
            await _dbContext.SaveChangesAsync();

            return new ServiceResponse<TransactionDto>
            {
                Success = true,
                Message = "Transaction completed successfully",
                Data = new TransactionDto
                {
                    Id = transaction.Id,
                    TransactionDate =
                        transaction.TransactionDate,
                    Description =
                        request.Description,
                    Amount =
                        request.Amount,
                    Type =
                        request.Type,
                    Status =
                        TransactionStatus.Completed,
                    RecipientName =
                        string.IsNullOrWhiteSpace(request.RecipientName)
                            ? null
                            : request.RecipientName,
                    RecipientRegistrationNumber =
                        string.IsNullOrWhiteSpace(request.RecipientRegistrationNumber)
                            ? null
                            : request.RecipientRegistrationNumber,
                    RecipientAccountNumber =
                        string.IsNullOrWhiteSpace(request.RecipientAccountNumber)
                            ? null
                            : request.RecipientAccountNumber
                }
            };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<TransactionDto>
            {
                Success = false,
                Message = $"Failed to create transaction: {ex.Message}"
            };
        }
    }

    public async Task<ServiceResponse<List<TransactionDto>>> GetAccountTransactionsAsync(Guid userId, Guid accountId)
    {
        try
        {
            var account = await _dbContext.BankAccounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

            if (account == null)
                return new ServiceResponse<List<TransactionDto>>
                {
                    Success = false,
                    Message = "Account not found"
                };

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return new ServiceResponse<List<TransactionDto>>
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            var (encryptionKey, _) =
                _encryptionService.DeriveKeyFromPassword(
                    user.EncryptionKeyHash,
                    user.EncryptionKeyHash);

            var transactions = await _dbContext.Transactions
                .Where(t => t.BankAccountId == accountId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            var decryptedTransactions = transactions.Select(t => DecryptTransaction(t, encryptionKey)).ToList();

            return new ServiceResponse<List<TransactionDto>>
            {
                Success = true,
                Data = decryptedTransactions
            };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<List<TransactionDto>>
            {
                Success = false,
                Message = $"Failed to retrieve transactions: {ex.Message}"
            };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteTransactionAsync(Guid userId, Guid transactionId)
    {
        try
        {
            var transaction = await _dbContext.Transactions
                .Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.BankAccount != null && t.BankAccount.UserId == userId);

            if (transaction == null)
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Transaction not found"
                };

            _dbContext.Transactions.Remove(transaction);
            await _dbContext.SaveChangesAsync();

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Transaction deleted successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = $"Failed to delete transaction: {ex.Message}"
            };
        }
    }
    
    public async Task<ServiceResponse<TransactionDto>> UpdateTransactionAsync(
        Guid userId,
        UpdateTransactionRequest request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var transaction = await _dbContext.Transactions
                .Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t =>
                    t.Id == request.Id &&
                    t.BankAccount != null &&
                    t.BankAccount.UserId == userId);

            if (transaction == null)
            {
                return new ServiceResponse<TransactionDto>
                {
                    Success = false,
                    Message = "Transaction not found"
                };
            }

            var user = await _dbContext.Users.FindAsync(userId);

            if (user == null)
            {
                return new ServiceResponse<TransactionDto>
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            var (encryptionKey, _) =
                _encryptionService.DeriveKeyFromPassword(
                    user.EncryptionKeyHash,
                    user.EncryptionKeyHash);

            // Current balance
            var (encryptedBalance, balanceIV) =
                ExtractEncryptedValue(transaction.BankAccount!.EncryptedBalance);

            var currentBalance =
                decimal.Parse(
                    _encryptionService.Decrypt(
                        encryptedBalance,
                        encryptionKey,
                        balanceIV),
                    CultureInfo.InvariantCulture);

            // Old transaction amount
            var (oldAmountEncrypted, oldAmountIV) =
                ExtractEncryptedValue(transaction.EncryptedAmount);

            var oldAmount =
                decimal.Parse(
                    _encryptionService.Decrypt(
                        oldAmountEncrypted,
                        encryptionKey,
                        oldAmountIV),
                    CultureInfo.InvariantCulture);

            // Reverse old transaction
            currentBalance -= CalculateBalanceChange(
                transaction.Type,
                oldAmount);

            // Apply new transaction
            currentBalance += CalculateBalanceChange(
                request.Type,
                request.Amount);

            if (currentBalance < 0)
            {
                return new ServiceResponse<TransactionDto>
                {
                    Success = false,
                    Message = "Insufficient funds"
                };
            }

            // Encrypt new balance
            var (newEncryptedBalance, newBalanceIV) =
                _encryptionService.Encrypt(
                    currentBalance.ToString(CultureInfo.InvariantCulture),
                    encryptionKey);

            transaction.BankAccount.EncryptedBalance =
                $"{newEncryptedBalance}:{newBalanceIV}";

            // Encrypt description
            var (encryptedDescription, descriptionIV) =
                _encryptionService.Encrypt(
                    request.Description,
                    encryptionKey);

            transaction.EncryptedDescription =
                $"{encryptedDescription}:{descriptionIV}";

            // Encrypt amount
            var (encryptedAmount, amountIV) =
                _encryptionService.Encrypt(
                    request.Amount.ToString(CultureInfo.InvariantCulture),
                    encryptionKey);

            transaction.EncryptedAmount =
                $"{encryptedAmount}:{amountIV}";

            transaction.Type = request.Type;

            // Recipient info
            if (request.Type == TransactionType.Transfer)
            {
                var recipientName =
                    request.RecipientName?.Trim() ?? string.Empty;

                var recipientRegistration =
                    request.RecipientRegistrationNumber?.Trim() ?? string.Empty;

                var recipientAccount =
                    request.RecipientAccountNumber?.Trim() ?? string.Empty;

                var (encryptedRecipientName, recipientNameIV) =
                    _encryptionService.Encrypt(
                        recipientName,
                        encryptionKey);

                var (encryptedRecipientRegistration, recipientRegistrationIV) =
                    _encryptionService.Encrypt(
                        recipientRegistration,
                        encryptionKey);

                var (encryptedRecipientAccount, recipientAccountIV) =
                    _encryptionService.Encrypt(
                        recipientAccount,
                        encryptionKey);

                transaction.EncryptedRecipientName =
                    $"{encryptedRecipientName}:{recipientNameIV}";

                transaction.EncryptedRecipientRegistrationNumber =
                    $"{encryptedRecipientRegistration}:{recipientRegistrationIV}";

                transaction.EncryptedRecipientAccountNumber =
                    $"{encryptedRecipientAccount}:{recipientAccountIV}";
            }
            else
            {
                transaction.EncryptedRecipientName = string.Empty;
                transaction.EncryptedRecipientRegistrationNumber = string.Empty;
                transaction.EncryptedRecipientAccountNumber = string.Empty;
            }

            _dbContext.BankAccounts.Update(transaction.BankAccount);
            _dbContext.Transactions.Update(transaction);

            await _dbContext.SaveChangesAsync();

            return new ServiceResponse<TransactionDto>
            {
                Success = true,
                Message = "Transaction updated successfully",
                Data = new TransactionDto
                {
                    Id = transaction.Id,
                    TransactionDate = transaction.TransactionDate,
                    Description = request.Description,
                    Amount = request.Amount,
                    Type = request.Type,
                    Status = transaction.Status,
                    RecipientName = request.Type == TransactionType.Transfer
                        ? request.RecipientName
                        : null,
                    RecipientRegistrationNumber = request.Type == TransactionType.Transfer
                        ? request.RecipientRegistrationNumber
                        : null,
                    RecipientAccountNumber = request.Type == TransactionType.Transfer
                        ? request.RecipientAccountNumber
                        : null
                }
            };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<TransactionDto>
            {
                Success = false,
                Message = $"Failed to update transaction: {ex.Message}"
            };
        }
    }
    
    private TransactionDto DecryptTransaction(
        Transaction transaction,
        byte[] encryptionKey)
    {
        var (description, descIV) =
            ExtractEncryptedValue(transaction.EncryptedDescription);

        var (amount, amountIV) =
            ExtractEncryptedValue(transaction.EncryptedAmount);
        
        string? decryptedRecipientName = null;
        string? decryptedRecipientRegistration = null;
        string? decryptedRecipientAccount = null;

        if (transaction.Type == TransactionType.Transfer)
        {
            var (recipientName, recipientNameIV) =
                ExtractEncryptedValue(transaction.EncryptedRecipientName);

            var (recipientRegistration, recipientRegistrationIV) =
                ExtractEncryptedValue(transaction.EncryptedRecipientRegistrationNumber);

            var (recipientAccount, recipientAccountIV) =
                ExtractEncryptedValue(transaction.EncryptedRecipientAccountNumber);

            decryptedRecipientName =
                _encryptionService.Decrypt(
                    recipientName,
                    encryptionKey,
                    recipientNameIV);

            decryptedRecipientRegistration =
                _encryptionService.Decrypt(
                    recipientRegistration,
                    encryptionKey,
                    recipientRegistrationIV);

            decryptedRecipientAccount =
                _encryptionService.Decrypt(
                    recipientAccount,
                    encryptionKey,
                    recipientAccountIV);
        }
        
        return new TransactionDto
        {
            Id = transaction.Id,
            TransactionDate =
                transaction.TransactionDate,
            Description =
                _encryptionService.Decrypt(
                    description,
                    encryptionKey,
                    descIV),
            Amount =
                decimal.Parse(
                    _encryptionService.Decrypt(
                        amount,
                        encryptionKey,
                        amountIV),
                    CultureInfo.InvariantCulture),
            Type =
                transaction.Type,
            Status =
                transaction.Status,
            RecipientName =
                string.IsNullOrWhiteSpace(decryptedRecipientName)
                    ? null
                    : decryptedRecipientName,
            RecipientRegistrationNumber =
                string.IsNullOrWhiteSpace(decryptedRecipientRegistration)
                    ? null
                    : decryptedRecipientRegistration,
            RecipientAccountNumber =
                string.IsNullOrWhiteSpace(decryptedRecipientAccount)
                    ? null
                    : decryptedRecipientAccount
        };
    }
    
    private decimal CalculateBalanceChange(
        TransactionType type,
        decimal amount)
    {
        return type switch
        {
            TransactionType.Deposit => +amount,
            TransactionType.Received => +amount,
            
            TransactionType.Credit => -amount,
            TransactionType.Withdraw => -amount,
            TransactionType.Debit => -amount,
            TransactionType.Purchase => -amount,

            _ => throw new ArgumentException(
                $"Invalid transaction type: {type}")
        };
    }
    
    private (string encryptedValue, string iv) ExtractEncryptedValue(string encryptedData)
    {
        var parts = encryptedData.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new InvalidOperationException("Encrypted data is malformed.");
        }

        return (parts[0], parts[1]);
    }
}