using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Vault.Domain.Entities;
using Vault.Domain.Persistence;
using Vault.Service.DTOs;
using Vault.Service.Interfaces;
using Vault.Service.Security;

namespace Vault.Service.Services;

public class BankAccountService : IBankAccountService
{
    private readonly VaultDbContext _dbContext;
    private readonly IEncryptionService _encryptionService;

    public BankAccountService(
        VaultDbContext dbContext,
        IEncryptionService encryptionService)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
    }

    public async Task<ServiceResponse<BankAccountDetailDto>> CreateAccountAsync(int userId, CreateBankAccountRequest request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
                return new ServiceResponse<BankAccountDetailDto>
                {
                    Success = false,
                    Message = "User not found"
                };

            var (encryptionKey, _) = _encryptionService.DeriveKeyFromPassword(
                user.EncryptionKeyHash,
                user.EncryptionKeyHash);

            var (encryptedAccountNumber, accountNumberIV) = _encryptionService.Encrypt(request.AccountNumber, encryptionKey);
            var (encryptedIBAN, ibanIV) = _encryptionService.Encrypt(request.IBAN, encryptionKey);
            var (encryptedBalance, balanceIV) = _encryptionService.Encrypt(request.Balance.ToString(CultureInfo.InvariantCulture), encryptionKey);
            var (encryptedCurrency, currencyIV) = _encryptionService.Encrypt(request.Currency, encryptionKey);
            var (encryptedBankName, bankNameIV) = _encryptionService.Encrypt(request.BankName, encryptionKey);

            var account = new BankAccount
            {
                UserId = userId,
                AccountName = request.AccountName,
                EncryptedAccountNumber = $"{encryptedAccountNumber}:{accountNumberIV}",
                EncryptedIBAN = $"{encryptedIBAN}:{ibanIV}",
                EncryptedBalance = $"{encryptedBalance}:{balanceIV}",
                EncryptedCurrency = $"{encryptedCurrency}:{currencyIV}",
                EncryptedBankName = $"{encryptedBankName}:{bankNameIV}",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.BankAccounts.Add(account);
            await _dbContext.SaveChangesAsync();

            return new ServiceResponse<BankAccountDetailDto>
            {
                Success = true,
                Message = "Bank account created successfully",
                Data = new BankAccountDetailDto
                {
                    Id = account.Id,
                    AccountName = account.AccountName,
                    AccountNumber = request.AccountNumber,
                    IBAN = request.IBAN,
                    Balance = request.Balance,
                    Currency = request.Currency,
                    BankName = request.BankName,
                    CreatedAt = account.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<BankAccountDetailDto>
            {
                Success = false,
                Message = $"Failed to create account: {ex.Message}"
            };
        }
    }

    public async Task<ServiceResponse<BankAccountDetailDto>> GetAccountAsync(int userId, int accountId)
    {
        try
        {
            var account = await _dbContext.BankAccounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

            if (account == null)
                return new ServiceResponse<BankAccountDetailDto>
                {
                    Success = false,
                    Message = "Account not found"
                };

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return new ServiceResponse<BankAccountDetailDto>
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            var (encryptionKey, _) = _encryptionService.DeriveKeyFromPassword(
                user.EncryptionKeyHash,
                user.EncryptionKeyHash);

            var decryptedData = DecryptAccountData(account, encryptionKey);

            return new ServiceResponse<BankAccountDetailDto>
            {
                Success = true,
                Data = decryptedData
            };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<BankAccountDetailDto>
            {
                Success = false,
                Message = $"Failed to retrieve account: {ex.Message}"
            };
        }
    }

    public async Task<ServiceResponse<List<BankAccountDto>>> GetAllAccountsAsync(int userId)
    {
        try
        {
            var accounts = await _dbContext.BankAccounts
                .Where(a => a.UserId == userId)
                .Select(a => new BankAccountDto
                {
                    Id = a.Id,
                    AccountName = a.AccountName,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return new ServiceResponse<List<BankAccountDto>>
            {
                Success = true,
                Data = accounts
            };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<List<BankAccountDto>>
            {
                Success = false,
                Message = $"Failed to retrieve accounts: {ex.Message}"
            };
        }
    }

    public async Task<ServiceResponse<BankAccountDetailDto>> UpdateAccountAsync(int userId, UpdateBankAccountRequest request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var account = await _dbContext.BankAccounts
                .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == userId);

            if (account == null)
                return new ServiceResponse<BankAccountDetailDto>
                {
                    Success = false,
                    Message = "Account not found"
                };

            account.AccountName = request.AccountName;

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return new ServiceResponse<BankAccountDetailDto>
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            var (encryptionKey, _) = _encryptionService.DeriveKeyFromPassword(
                user.EncryptionKeyHash,
                user.EncryptionKeyHash);

            var (encryptedBalance, balanceIV) = _encryptionService.Encrypt(request.Balance.ToString(CultureInfo.InvariantCulture), encryptionKey);
            account.EncryptedBalance = $"{encryptedBalance}:{balanceIV}";

            _dbContext.BankAccounts.Update(account);
            await _dbContext.SaveChangesAsync();

            var decryptedData = DecryptAccountData(account, encryptionKey);

            return new ServiceResponse<BankAccountDetailDto>
            {
                Success = true,
                Message = "Account updated successfully",
                Data = decryptedData
            };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<BankAccountDetailDto>
            {
                Success = false,
                Message = $"Failed to update account: {ex.Message}"
            };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAccountAsync(int userId, int accountId)
    {
        try
        {
            var account = await _dbContext.BankAccounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

            if (account == null)
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Account not found"
                };

            _dbContext.BankAccounts.Remove(account);
            await _dbContext.SaveChangesAsync();

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Account deleted successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = $"Failed to delete account: {ex.Message}"
            };
        }
    }

    public async Task<ServiceResponse<TransactionDto>> CreateTransactionAsync(int userId, CreateTransactionRequest request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);

            var account = await _dbContext.BankAccounts
                .FirstOrDefaultAsync(a => a.Id == request.BankAccountId && a.UserId == userId);

            if (account == null)
                return new ServiceResponse<TransactionDto>
                {
                    Success = false,
                    Message = "Account not found"
                };

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return new ServiceResponse<TransactionDto>
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            var (encryptionKey, _) = _encryptionService.DeriveKeyFromPassword(
                user.EncryptionKeyHash,
                user.EncryptionKeyHash);

            var (encryptedDescription, descIV) = _encryptionService.Encrypt(request.Description, encryptionKey);
            var (encryptedAmount, amountIV) = _encryptionService.Encrypt(request.Amount.ToString(CultureInfo.InvariantCulture), encryptionKey);
            var (encryptedType, typeIV) = _encryptionService.Encrypt(request.Type, encryptionKey);
            var recipient = request.Recipient?.Trim();
            var (encryptedRecipient, recipientIV) = _encryptionService.Encrypt(recipient ?? string.Empty, encryptionKey);

            var transaction = new Transaction
            {
                BankAccountId = request.BankAccountId,
                TransactionDate = DateTime.UtcNow,
                EncryptedDescription = $"{encryptedDescription}:{descIV}",
                EncryptedAmount = $"{encryptedAmount}:{amountIV}",
                EncryptedType = $"{encryptedType}:{typeIV}",
                EncryptedRecipient = $"{encryptedRecipient}:{recipientIV}"
            };

            _dbContext.Transactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            return new ServiceResponse<TransactionDto>
            {
                Success = true,
                Message = "Transaction created successfully",
                Data = new TransactionDto
                {
                    Id = transaction.Id,
                    TransactionDate = transaction.TransactionDate,
                    Description = request.Description,
                    Amount = request.Amount,
                    Type = request.Type,
                    Recipient = string.IsNullOrWhiteSpace(recipient) ? null : recipient
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

    public async Task<ServiceResponse<List<TransactionDto>>> GetAccountTransactionsAsync(int userId, int accountId)
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

            var (encryptionKey, _) = _encryptionService.DeriveKeyFromPassword(user.EncryptionKeyHash);

            var transactions = await _dbContext.Transactions
                .Where(t => t.BankAccountId == accountId)
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

    public async Task<ServiceResponse<bool>> DeleteTransactionAsync(int userId, int transactionId)
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

    private BankAccountDetailDto DecryptAccountData(BankAccount account, byte[] encryptionKey)
    {
        var (accountNumber, accountNumberIV) = ExtractEncryptedValue(account.EncryptedAccountNumber);
        var (iban, ibanIV) = ExtractEncryptedValue(account.EncryptedIBAN);
        var (balance, balanceIV) = ExtractEncryptedValue(account.EncryptedBalance);
        var (currency, currencyIV) = ExtractEncryptedValue(account.EncryptedCurrency);
        var (bankName, bankNameIV) = ExtractEncryptedValue(account.EncryptedBankName);

        return new BankAccountDetailDto
        {
            Id = account.Id,
            AccountName = account.AccountName,
            AccountNumber = _encryptionService.Decrypt(accountNumber, encryptionKey, accountNumberIV),
            IBAN = _encryptionService.Decrypt(iban, encryptionKey, ibanIV),
            Balance = decimal.Parse(_encryptionService.Decrypt(balance, encryptionKey, balanceIV), CultureInfo.InvariantCulture),
            Currency = _encryptionService.Decrypt(currency, encryptionKey, currencyIV),
            BankName = _encryptionService.Decrypt(bankName, encryptionKey, bankNameIV),
            CreatedAt = account.CreatedAt
        };
    }

    private TransactionDto DecryptTransaction(Transaction transaction, byte[] encryptionKey)
    {
        var (description, descIV) = ExtractEncryptedValue(transaction.EncryptedDescription);
        var (amount, amountIV) = ExtractEncryptedValue(transaction.EncryptedAmount);
        var (type, typeIV) = ExtractEncryptedValue(transaction.EncryptedType);
        var (recipient, recipientIV) = ExtractEncryptedValue(transaction.EncryptedRecipient);
        var decryptedRecipient = _encryptionService.Decrypt(recipient, encryptionKey, recipientIV);

        return new TransactionDto
        {
            Id = transaction.Id,
            TransactionDate = transaction.TransactionDate,
            Description = _encryptionService.Decrypt(description, encryptionKey, descIV),
            Amount = decimal.Parse(_encryptionService.Decrypt(amount, encryptionKey, amountIV), CultureInfo.InvariantCulture),
            Type = _encryptionService.Decrypt(type, encryptionKey, typeIV),
            Recipient = string.IsNullOrWhiteSpace(decryptedRecipient)
                ? null
                : decryptedRecipient
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
