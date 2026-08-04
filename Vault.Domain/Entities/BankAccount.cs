namespace Vault.Domain.Entities
{
    public class BankAccount
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        
        // Not encrypted
        public string AccountName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Encrypted (format: Base64Ciphertext:Base64IV)
        public string EncryptedAccountNumber { get; set; } = string.Empty;
        public string EncryptedIBAN { get; set; } = string.Empty;
        public string EncryptedBalance { get; set; } = string.Empty;
        public string EncryptedCurrency { get; set; } = string.Empty;
        public string EncryptedBankName { get; set; } = string.Empty;
        
        // Navigation
        public User? User { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
