namespace FileProcessingService.Api.Security;

public sealed class ManagedClientKeysSettings
{
    public bool RequireExpiration { get; set; } = true;
    public int DefaultLifetimeDays { get; set; } = 90;
    public int MaximumLifetimeDays { get; set; } = 365;
    public int RotationOverlapDays { get; set; } = 7;
    public int MaximumActiveKeysPerClient { get; set; } = 2;
}
