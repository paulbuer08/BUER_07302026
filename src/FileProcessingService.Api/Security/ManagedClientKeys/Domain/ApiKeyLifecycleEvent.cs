namespace FileProcessingService.Api.Security.ManagedClientKeys.Domain;

public sealed class ApiKeyLifecycleEvent
{
    public Guid Id { get; internal set; }
    public Guid ApiKeyCredentialId { get; internal set; }
    public ApiKeyLifecycleEventType EventType { get; internal set; }
    public DateTimeOffset OccurredAtUtc { get; internal set; }

    public string ActorIdentifier { get; internal set; } = string.Empty;
    public string? Reason { get; internal set; }
    public string? MetadataJson { get; internal set; }

    public ApiKeyCredential ApiKeyCredential { get; internal set; } = null!;
}
