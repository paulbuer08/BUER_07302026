namespace FileProcessingService.Api.Security.ManagedClientKeys.Credentials;

internal sealed record GeneratedManagedClientApiKey(
    string KeyId,
    string RawApiKey,
    byte[] SecretHash,
    short HashAlgorithmVersion);
