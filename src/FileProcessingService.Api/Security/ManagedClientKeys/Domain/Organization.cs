namespace FileProcessingService.Api.Security.ManagedClientKeys.Domain;

public sealed class Organization
{
    public Guid Id { get; internal set; }
    public string Code { get; internal set; } = string.Empty;
    public string NormalizedCode { get; internal set; } = string.Empty;
    public string DisplayName { get; internal set; } = string.Empty;

    public OrganizationStatus Status { get; internal set; } = OrganizationStatus.PendingApproval;
    public DateTimeOffset CreatedAtUtc { get; internal set; }
    public DateTimeOffset? ApprovedAtUtc { get; internal set; }
    public DateTimeOffset? SuspendedAtUtc { get; internal set; }
    public DateTimeOffset? ClosedAtUtc { get; internal set; }

    public ICollection<ClientApplication> ClientApplications { get; } = [];
}
