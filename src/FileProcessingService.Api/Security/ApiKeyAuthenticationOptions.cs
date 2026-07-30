using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Security;


public sealed class ApiKeyAuthenticationOptions
{
    public const string SectionName = "ApiKeyAuthentication";
    public string ApiKey { get; set; } = string.Empty;
}


public static class ApiKeyAuthenticationDefaults
{
    public const string HeaderName = "X-Api-Key";
    public static PathString ProtectedPathPrefix { get; } = new("/api/v1/files");
}



public sealed class ApiKeyAuthenticationOptionsValidator
    : IValidateOptions<ApiKeyAuthenticationOptions>
{
    private const int MinimumLength = 32;
    private const int MaximumLength = 128;

    public ValidateOptionsResult Validate(string? name, ApiKeyAuthenticationOptions options)
    {
        if (string.IsNullOrEmpty(options.ApiKey))
        {
            return ValidateOptionsResult.Fail(
                "ApiKeyAuthentication:ApiKey is required.");
        }
        if (options.ApiKey.Length is < MinimumLength or > MaximumLength)
        {
            return ValidateOptionsResult.Fail(
                $"ApiKeyAuthentication:ApiKey must contain between " +
                $"{MinimumLength} and {MaximumLength} characters.");
        }
        if (options.ApiKey.Any(character => character is < '!' or > '~'))
        {
            return ValidateOptionsResult.Fail(
                "ApiKeyAuthentication:ApiKey must contain only " +
                "visible ASCII characters without spaces.");
        }

        return ValidateOptionsResult.Success;
    }

}
