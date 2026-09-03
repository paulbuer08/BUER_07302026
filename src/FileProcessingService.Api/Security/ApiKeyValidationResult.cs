namespace FileProcessingService.Api.Security;


public sealed record ApiKeyValidationResult(
    bool Succeeded,
    string? ClientId,
    IReadOnlyCollection<string> Scopes)
{
    public static ApiKeyValidationResult Failed { get; } = new(false, null, []);


    public static ApiKeyValidationResult Success(string clientId, params string[] scopes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        return new(true, clientId, Array.AsReadOnly(scopes));
    }
}
