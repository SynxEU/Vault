using Vault.Domain.Entities.Enums;

namespace Vault.Domain.Entities
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public Guid BankAccountId { get; set; }
        
        // Not encrypted
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public TransactionStatus Status { get; set; }
        public TransactionType Type { get; set; }
        
        // Encrypted (format: Base64Ciphertext:Base64IV)
        public string EncryptedDescription { get; set; } = string.Empty;
        public string EncryptedAmount { get; set; } = string.Empty;
        
        // Recipient information
        public string EncryptedRecipientName { get; set; } = string.Empty;

        public string EncryptedRecipientRegistrationNumber { get; set; } = string.Empty;

        public string EncryptedRecipientAccountNumber { get; set; } = string.Empty;
        
        // Navigation
        public BankAccount? BankAccount { get; set; }
    }
}
