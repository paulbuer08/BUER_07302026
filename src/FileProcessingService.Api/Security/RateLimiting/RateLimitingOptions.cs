namespace FileProcessingService.Api.Security.RateLimiting;


public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";
    public const string PolicyName = "file-processing";

    public int PermitLimit { get; set; } = 20;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; }
}
