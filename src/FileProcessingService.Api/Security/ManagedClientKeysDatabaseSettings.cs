namespace FileProcessingService.Api.Security;

public sealed class ManagedClientKeysDatabaseSettings
{
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int MaximumRetryCount { get; set; } = 3;
    public int MaximumRetryDelaySeconds { get; set; } = 5;
}
