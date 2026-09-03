using Microsoft.EntityFrameworkCore;

namespace FileProcessingService.Api.Security.ManagedClientKeys.Persistence;


internal static class ManagedClientKeysDbContextOptionsExtensions
{
    public static DbContextOptionsBuilder UseManagedClientKeysDatabase(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString)
    {
        return optionsBuilder.UseManagedClientKeysDatabase(
            connectionString,
            new ManagedClientKeysDatabaseSettings());
    }



    public static DbContextOptionsBuilder UseManagedClientKeysDatabase(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        ManagedClientKeysDatabaseSettings databaseSettings)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(databaseSettings);

        return optionsBuilder.UseNpgsql(
            connectionString,
            providerOptions =>
            {
                providerOptions.MigrationsHistoryTable(
                    ManagedClientKeysDatabaseDefaults.MigrationHistoryTableName,
                    ManagedClientKeysDatabaseDefaults.MigrationSchemaName);

                providerOptions.CommandTimeout(databaseSettings.CommandTimeoutSeconds);

                if (databaseSettings.MaximumRetryCount > 0)
                {
                    providerOptions.EnableRetryOnFailure(
                        databaseSettings.MaximumRetryCount,
                        TimeSpan.FromSeconds(databaseSettings.MaximumRetryDelaySeconds),
                        null);
                }
            });
    }
}
