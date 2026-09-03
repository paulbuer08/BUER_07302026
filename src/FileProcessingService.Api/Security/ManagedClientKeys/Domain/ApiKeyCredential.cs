namespace FileProcessingService.Api.Security.ManagedClientKeys.Domain;

public sealed class ApiKeyCredential
{
    public Guid Id { get; internal set; }
    public Guid ClientApplicationId { get; internal set; }
    public string KeyId { get; internal set; } = string.Empty;

    internal byte[] SecretHash { get; set; } = [];
    public short HashAlgorithmVersion { get; internal set; } = 1;

    public DateTimeOffset IssuedAtUtc { get; internal set; }
    public DateTimeOffset NotBeforeUtc { get; internal set; }
    public DateTimeOffset? ExpiresAtUtc { get; internal set; }
    public DateTimeOffset? RevokedAtUtc { get; internal set; }
    public string? RevocationReason { get; internal set; }
    public Guid? ReplacesCredentialId { get; internal set; }

    public ClientApplication ClientApplication { get; internal set; } = null!;
    public ApiKeyCredential? ReplacesCredential { get; internal set; }
    public ApiKeyCredential? ReplacementCredential { get; internal set; }

    public ICollection<ApiKeyCredentialScope> Scopes { get; } = [];
    public ICollection<ApiKeyLifecycleEvent> LifecycleEvents { get; } = [];
}
