namespace FileProcessingService.Api.Security.ManagedClientKeys.Domain;

public sealed class ClientApplication
{
    public Guid Id { get; internal set; }
    public Guid OrganizationId { get; internal set; }
    public string ClientIdentifier { get; internal set; } = string.Empty;
    public string DisplayName { get; internal set; } = string.Empty;

    public ClientApplicationStatus Status { get; internal set; } = ClientApplicationStatus.PendingActivation;
    public DateTimeOffset CreatedAtUtc { get; internal set; }
    public DateTimeOffset? ActivatedAtUtc { get; internal set; }
    public DateTimeOffset? SuspendedAtUtc { get; internal set; }
    public DateTimeOffset? DisabledAtUtc { get; internal set; }

    public Organization Organization { get; internal set; } = null!;
    public ICollection<ApiKeyCredential> ApiKeyCredentials { get; } = [];
}
