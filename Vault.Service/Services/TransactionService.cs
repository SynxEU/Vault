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
    IDbContextTransaction? dbTransaction = null;

    if (_dbContext.Database.IsRelational())
    {
        dbTransaction = await _dbContext.Database.BeginTransactionAsync();
    }

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

        // Encrypt recipient information

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
            EncryptedRecipientName =
                $"{encryptedRecipientName}:{recipientNameIV}",
            EncryptedRecipientRegistrationNumber =
                $"{encryptedRecipientRegistration}:{recipientRegistrationIV}",
            EncryptedRecipientAccountNumber =
                $"{encryptedRecipientAccount}:{recipientAccountIV}"
        };
        
        _dbContext.Transactions.Add(transaction);
        
        await _dbContext.SaveChangesAsync();
        if (dbTransaction != null)
        {
            await dbTransaction.CommitAsync();
        }
        
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
                    string.IsNullOrWhiteSpace(recipientName)
                        ? null
                        : recipientName,
                RecipientRegistrationNumber =
                    string.IsNullOrWhiteSpace(recipientRegistration)
                        ? null
                        : recipientRegistration,
                RecipientAccountNumber =
                    string.IsNullOrWhiteSpace(recipientAccount)
                        ? null
                        : recipientAccount
            }
        };
    }
    catch (Exception ex)
    {
        if (dbTransaction != null)
        {
            await dbTransaction.RollbackAsync();
        }

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
    
    private TransactionDto DecryptTransaction(
        Transaction transaction,
        byte[] encryptionKey)
    {
        var (description, descIV) =
            ExtractEncryptedValue(transaction.EncryptedDescription);

        var (amount, amountIV) =
            ExtractEncryptedValue(transaction.EncryptedAmount);
        
        var (recipientName, recipientNameIV) =
            ExtractEncryptedValue(
                transaction.EncryptedRecipientName);

        var (recipientRegistration, recipientRegistrationIV) =
            ExtractEncryptedValue(
                transaction.EncryptedRecipientRegistrationNumber);

        var (recipientAccount, recipientAccountIV) =
            ExtractEncryptedValue(
                transaction.EncryptedRecipientAccountNumber);
        
        var decryptedRecipientName =
            _encryptionService.Decrypt(
                recipientName,
                encryptionKey,
                recipientNameIV);

        var decryptedRecipientRegistration =
            _encryptionService.Decrypt(
                recipientRegistration,
                encryptionKey,
                recipientRegistrationIV);
        
        var decryptedRecipientAccount =
            _encryptionService.Decrypt(
                recipientAccount,
                encryptionKey,
                recipientAccountIV);
        
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
            TransactionType.Deposit => amount,
            TransactionType.Received => amount,
            TransactionType.Credit => amount,

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