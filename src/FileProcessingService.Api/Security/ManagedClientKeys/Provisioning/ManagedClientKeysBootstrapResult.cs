namespace FileProcessingService.Api.Security.ManagedClientKeys.Provisioning;

public abstract record ManagedClientKeysBootstrapResult(
    Guid OrganizationId,
    Guid ClientApplicationId,
    Guid ApiKeyCredentialId,
    string KeyId)
{
    public sealed record Created(
        Guid OrganizationId,
        Guid ClientApplicationId,
        Guid ApiKeyCredentialId,
        string KeyId,
        string RawApiKey)
        : ManagedClientKeysBootstrapResult(
            OrganizationId,
            ClientApplicationId,
            ApiKeyCredentialId,
            KeyId);

    public sealed record AlreadyExists(
        Guid OrganizationId,
        Guid ClientApplicationId,
        Guid ApiKeyCredentialId,
        string KeyId)
        : ManagedClientKeysBootstrapResult(
            OrganizationId,
            ClientApplicationId,
            ApiKeyCredentialId,
            KeyId);
}
