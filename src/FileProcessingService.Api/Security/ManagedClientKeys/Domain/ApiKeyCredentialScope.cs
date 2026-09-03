namespace FileProcessingService.Api.Security.ManagedClientKeys.Domain;

public sealed class ApiKeyCredentialScope
{
    public Guid ApiKeyCredentialId { get; internal set; }
    public string Scope { get; internal set; } = string.Empty;
    public ApiKeyCredential ApiKeyCredential { get; internal set; } = null!;
}
