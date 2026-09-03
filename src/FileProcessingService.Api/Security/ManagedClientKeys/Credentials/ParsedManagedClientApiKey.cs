namespace FileProcessingService.Api.Security.ManagedClientKeys.Credentials;

internal readonly record struct ParsedManagedClientApiKey(
    string KeyId,
    ReadOnlyMemory<char> Secret);
