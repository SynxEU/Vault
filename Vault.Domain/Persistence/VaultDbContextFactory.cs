using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vault.Domain.Persistence;

public sealed class VaultDbContextFactory : IDesignTimeDbContextFactory<VaultDbContext>
{
    public VaultDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VaultDbContext>();

        // Keep the design-time connection aligned with the app's default fallback.
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=VaultDb;Trusted_Connection=true;";
        optionsBuilder.UseSqlServer(connectionString);

        return new VaultDbContext(optionsBuilder.Options);
    }
}
