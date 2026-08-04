namespace Vault.Domain.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public int BankAccountId { get; set; }
        
        // Not encrypted
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        
        // Encrypted (format: Base64Ciphertext:Base64IV)
        public string EncryptedDescription { get; set; } = string.Empty;
        public string EncryptedAmount { get; set; } = string.Empty;
        public string EncryptedType { get; set; } = string.Empty;
        public string EncryptedRecipient { get; set; } = string.Empty;
        
        // Navigation
        public BankAccount? BankAccount { get; set; }
    }
}
