using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FileProcessingService.Api.Security.ManagedClientKeys.Persistence;


public sealed class ManagedClientKeysDbContextFactory
    : IDesignTimeDbContextFactory<ManagedClientKeysDbContext>
{
    public ManagedClientKeysDbContext CreateDbContext(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var connectionString = Environment.GetEnvironmentVariable(ManagedClientKeysDatabaseDefaults.ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{ManagedClientKeysDatabaseDefaults.ConnectionStringEnvironmentVariable} " +
                "must be provided when using EF Core design-time commands.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ManagedClientKeysDbContext>();
        optionsBuilder.UseManagedClientKeysDatabase(connectionString);

        return new ManagedClientKeysDbContext(optionsBuilder.Options);
    }
}
