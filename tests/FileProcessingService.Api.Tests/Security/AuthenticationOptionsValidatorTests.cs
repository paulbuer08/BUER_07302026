using FileProcessingService.Api.Security;

namespace FileProcessingService.Api.Tests.Security;


public sealed class AuthenticationOptionsValidatorTests
{
    private const string ValidSharedKey = "Unit-Test-Shared-Key-2026-1234567890!";
    private readonly AuthenticationOptionsValidator _validator = new();


    [Fact]
    public void SupportedModesMatchesDocumentedConfigurationOptions()
    {
        Assert.Equal(
            "SharedKey|ManagedClientKeys",
            AuthenticationOptions.SupportedModes);
    }



    [Fact]
    public void SharedKeyModeWithValidConfigurationSucceeds()
    {
        var options = CreateValidSharedKeyOptions();
        var result = _validator.Validate(null, options);
        Assert.True(result.Succeeded);
    }



    [Fact]
    public void SharedKeyModeWithoutSharedKeyFails()
    {
        var options = CreateValidSharedKeyOptions();
        options.SharedKey = string.Empty;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "Authentication:SharedKey is required",
                StringComparison.Ordinal));
    }



    [Fact]
    public void ManagedClientKeysModeDoesNotRequireSharedKey()
    {
        var options = new AuthenticationOptions
        {
            Mode = AuthenticationMode.ManagedClientKeys,
            HeaderName = "X-Api-Key",
            SharedKey = string.Empty,
            ManagedClientKeysSettings = new ManagedClientKeysSettings()
        };

        var result = _validator.Validate(null, options);
        Assert.True(result.Succeeded);
    }



    [Fact]
    public void ManagedClientKeysModeWithInvalidRotationPolicyFails()
    {
        var options = new AuthenticationOptions
        {
            Mode = AuthenticationMode.ManagedClientKeys,
            HeaderName = "X-Api-Key",
            ManagedClientKeysSettings = new ManagedClientKeysSettings
            {
                RotationOverlapDays = 7,
                MaximumActiveKeysPerClient = 1
            }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "MaximumActiveKeysPerClient must be at least 2",
                StringComparison.Ordinal));
    }



    [Fact]
    public void InvalidHeaderNameFails()
    {
        var options = CreateValidSharedKeyOptions();
        options.HeaderName = "Invalid Header Name";

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "Authentication:HeaderName",
                StringComparison.Ordinal));
    }



    [Fact]
    public void UndefinedAuthenticationModeFails()
    {
        var options = CreateValidSharedKeyOptions();
        options.Mode = (AuthenticationMode)999;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "Authentication:Mode",
                StringComparison.Ordinal));
    }



    [Theory]
    [InlineData(0, 3, 5, "CommandTimeoutSeconds")]
    [InlineData(30, -1, 5, "MaximumRetryCount")]
    [InlineData(30, 3, 0, "MaximumRetryDelaySeconds")]
    public void ManagedClientKeysModeWithInvalidDatabaseSettingsFails(
        int commandTimeoutSeconds,
        int maximumRetryCount,
        int maximumRetryDelaySeconds,
        string expectedSettingName)
    {
        var options = new AuthenticationOptions
        {
            Mode = AuthenticationMode.ManagedClientKeys,
            HeaderName = "X-Api-Key",
            ManagedClientKeysSettings = new ManagedClientKeysSettings
            {
                Database = new ManagedClientKeysDatabaseSettings
                {
                    CommandTimeoutSeconds = commandTimeoutSeconds,
                    MaximumRetryCount = maximumRetryCount,
                    MaximumRetryDelaySeconds = maximumRetryDelaySeconds
                }
            }
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                expectedSettingName,
                StringComparison.Ordinal));
    }



    private static AuthenticationOptions CreateValidSharedKeyOptions() =>
        new()
        {
            Mode = AuthenticationMode.SharedKey,
            HeaderName = "X-Api-Key",
            SharedKey = ValidSharedKey
        };
}
