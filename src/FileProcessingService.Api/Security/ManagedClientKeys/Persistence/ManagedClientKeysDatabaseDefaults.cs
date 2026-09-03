namespace FileProcessingService.Api.Security.ManagedClientKeys.Persistence;


internal static class ManagedClientKeysDatabaseDefaults
{
    public const string ConnectionStringName = "ManagedClientKeys";
    public const string ConnectionStringEnvironmentVariable = "ConnectionStrings__ManagedClientKeys";

    public const string MigrationSchemaName = "migration";
    public const string MigrationHistoryTableName = "__EFMigrationsHistory";
}
