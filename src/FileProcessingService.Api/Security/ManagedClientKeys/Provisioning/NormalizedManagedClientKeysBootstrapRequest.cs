namespace FileProcessingService.Api.Security.ManagedClientKeys.Provisioning;

internal sealed record NormalizedManagedClientKeysBootstrapRequest(
    string OrganizationCode,
    string NormalizedOrganizationCode,
    string OrganizationDisplayName,
    string ClientIdentifier,
    string ClientDisplayName,
    IReadOnlyList<string> Scopes,
    string ActorIdentifier);
