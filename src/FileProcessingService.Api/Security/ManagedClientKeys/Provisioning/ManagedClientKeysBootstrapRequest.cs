namespace FileProcessingService.Api.Security.ManagedClientKeys.Provisioning;

public sealed record ManagedClientKeysBootstrapRequest(
    string OrganizationCode,
    string OrganizationDisplayName,
    string ClientIdentifier,
    string ClientDisplayName,
    IReadOnlyCollection<string> Scopes,
    string ActorIdentifier);
