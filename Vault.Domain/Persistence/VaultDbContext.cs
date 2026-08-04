using Microsoft.EntityFrameworkCore;
using Vault.Domain.Entities;

namespace Vault.Domain.Persistence;

/// <summary>
/// Entity Framework DbContext for Vault application
/// </summary>
public class VaultDbContext : DbContext
{
    public VaultDbContext(DbContextOptions<VaultDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<BankAccount> BankAccounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PasswordHash)
                .IsRequired();

            entity.Property(e => e.EncryptionKeyHash)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.Username)
                .IsUnique();

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.HasMany(e => e.BankAccounts)
                .WithOne(b => b.User)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BankAccount Configuration
        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AccountName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.EncryptedAccountNumber)
                .IsRequired();

            entity.Property(e => e.EncryptedIBAN)
                .IsRequired();

            entity.Property(e => e.EncryptedBalance)
                .IsRequired();

            entity.Property(e => e.EncryptedCurrency)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.EncryptedBankName)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.UserId);

            entity.HasMany(e => e.Transactions)
                .WithOne(t => t.BankAccount)
                .HasForeignKey(t => t.BankAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Transaction Configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EncryptedDescription)
                .IsRequired();

            entity.Property(e => e.EncryptedAmount)
                .IsRequired();

            entity.Property(e => e.EncryptedType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.EncryptedRecipient);

            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.BankAccountId);
        });
    }
}
