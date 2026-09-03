using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Security;


public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";
    public const string SupportedModes = "SharedKey|ManagedClientKeys";
    public AuthenticationMode Mode { get; set; } = AuthenticationMode.SharedKey;
    public string HeaderName { get; set; } = "X-Api-Key";
    public string SharedKey { get; set; } = string.Empty;
    public ManagedClientKeysSettings ManagedClientKeysSettings { get; set; } = new();
}



public sealed class AuthenticationOptionsValidator
    : IValidateOptions<AuthenticationOptions>
{
    private const int MinimumSharedKeyLength = 32;
    private const int MaximumSharedKeyLength = 128;
    private const int MaximumAllowedLifetimeDays = 3650;
    private const int MaximumAllowedActiveKeys = 10;
    private const string ManagedClientKeysSettingsPath = $"{AuthenticationOptions.SectionName}:ManagedClientKeysSettings:";
    private const int MaximumDatabaseCommandTimeoutSeconds = 300;
    private const int MaximumDatabaseRetryCount = 10;
    private const int MaximumDatabaseRetryDelaySeconds = 60;
    private const string ManagedClientKeysDatabaseSettingsPath = $"{ManagedClientKeysSettingsPath}Database:";



    public ValidateOptionsResult Validate(string? name, AuthenticationOptions options)
    {
        var failures = new List<string>();

        ValidateMode(options, failures);
        ValidateHeaderName(options.HeaderName, failures);

        if (options.Mode == AuthenticationMode.SharedKey)
        {
            ValidateSharedKey(options.SharedKey, failures);
        }
        else if (options.Mode == AuthenticationMode.ManagedClientKeys)
        {
            ValidateManagedClientKeysSettings(options.ManagedClientKeysSettings, failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }



    private static void ValidateMode(AuthenticationOptions options, List<string> failures)
    {
        if (!Enum.IsDefined(options.Mode))
        {
            failures.Add(
                "Authentication:Mode must be SharedKey or ManagedClientKeys.");
        }
    }



    private static void ValidateHeaderName(string headerName, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(headerName)
            || headerName.Length > 128
            || !headerName.All(IsValidHeaderNameCharacter))
        {
            failures.Add(
                "Authentication:HeaderName must be a valid HTTP header name containing at most 128 characters.");
        }
    }



    private static bool IsValidHeaderNameCharacter(char character) =>
        char.IsAsciiLetterOrDigit(character)
        || character is '!'
            or '#'
            or '$'
            or '%'
            or '&'
            or '\''
            or '*'
            or '+'
            or '-'
            or '.'
            or '^'
            or '_'
            or '`'
            or '|'
            or '~';



    private static void ValidateSharedKey(string sharedKey, List<string> failures)
    {
        if (string.IsNullOrEmpty(sharedKey))
        {
            failures.Add("Authentication:SharedKey is required in SharedKey mode.");
            return;
        }

        if (sharedKey.Length is < MinimumSharedKeyLength or > MaximumSharedKeyLength)
        {
            failures.Add(
                $"Authentication:SharedKey must contain between " +
                $"{MinimumSharedKeyLength} and " +
                $"{MaximumSharedKeyLength} characters.");
        }

        if (sharedKey.Any(character => character is < '!' or > '~'))
        {
            failures.Add(
                "Authentication:SharedKey must contain only visible ASCII characters without spaces.");
        }
    }



    private static void ValidateManagedClientKeysSettings(ManagedClientKeysSettings settings, List<string> failures)
    {
        if (settings.DefaultLifetimeDays < 1)
        {
            failures.Add(
                ManagedClientKeysSettingsPath
                + "DefaultLifetimeDays must be at least 1.");
        }

        if (settings.MaximumLifetimeDays < settings.DefaultLifetimeDays)
        {
            failures.Add(
                ManagedClientKeysSettingsPath
                + "MaximumLifetimeDays must be greater than or equal to DefaultLifetimeDays.");
        }

        if (settings.MaximumLifetimeDays > MaximumAllowedLifetimeDays)
        {
            failures.Add(
                $"{ManagedClientKeysSettingsPath}"
                + $"MaximumLifetimeDays cannot exceed "
                + $"{MaximumAllowedLifetimeDays}.");
        }

        if (settings.RotationOverlapDays < 0 || settings.RotationOverlapDays > settings.MaximumLifetimeDays)
        {
            failures.Add(
                ManagedClientKeysSettingsPath
                + "RotationOverlapDays must be between 0 and MaximumLifetimeDays.");
        }

        if (settings.MaximumActiveKeysPerClient is < 1 or > MaximumAllowedActiveKeys)
        {
            failures.Add(
                $"{ManagedClientKeysSettingsPath}"
                + $"MaximumActiveKeysPerClient must be between 1 and "
                + $"{MaximumAllowedActiveKeys}.");
        }

        if (settings.RotationOverlapDays > 0 && settings.MaximumActiveKeysPerClient < 2)
        {
            failures.Add(
                ManagedClientKeysSettingsPath
                + "MaximumActiveKeysPerClient must be at least 2 when rotation overlap is enabled.");
        }

        ValidateManagedClientKeysDatabaseSettings(settings.Database, failures);
    }



    private static void ValidateManagedClientKeysDatabaseSettings(ManagedClientKeysDatabaseSettings settings, List<string> failures)
    {
        if (settings.CommandTimeoutSeconds is < 1 or > MaximumDatabaseCommandTimeoutSeconds)
        {
            failures.Add(
                $"{ManagedClientKeysDatabaseSettingsPath}"
                + $"CommandTimeoutSeconds must be between 1 and "
                + $"{MaximumDatabaseCommandTimeoutSeconds}.");
        }

        if (settings.MaximumRetryCount is < 0 or > MaximumDatabaseRetryCount)
        {
            failures.Add(
                $"{ManagedClientKeysDatabaseSettingsPath}"
                + $"MaximumRetryCount must be between 0 and "
                + $"{MaximumDatabaseRetryCount}.");
        }

        if (settings.MaximumRetryDelaySeconds is < 1 or > MaximumDatabaseRetryDelaySeconds)
        {
            failures.Add(
                $"{ManagedClientKeysDatabaseSettingsPath}"
                + $"MaximumRetryDelaySeconds must be between 1 and "
                + $"{MaximumDatabaseRetryDelaySeconds}.");
        }
    }

}
