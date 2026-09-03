using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileProcessingService.Api.Security.ManagedClientKeys.Provisioning;


internal static class ManagedClientKeysBootstrapCommand
{
    public const string CommandName = "bootstrap-managed-client";
    private const string SectionName = "ManagedClientKeysBootstrap";


    public static bool IsRequested(string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return arguments.Length > 0
            && string.Equals(
                arguments[0],
                CommandName,
                StringComparison.Ordinal);
    }



    public static async Task ExecuteAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        using var scope = services.CreateScope();

        var bootstrapper = scope.ServiceProvider
            .GetRequiredService<IManagedClientKeysBootstrapper>();

        var requestedScopes = configuration
            .GetSection($"{SectionName}:Scopes")
            .Get<string[]>()
            ?? [];

        var request = new ManagedClientKeysBootstrapRequest(
            GetRequiredValue(configuration, "OrganizationCode"),
            GetRequiredValue(configuration, "OrganizationDisplayName"),
            GetRequiredValue(configuration, "ClientIdentifier"),
            GetRequiredValue(configuration, "ClientDisplayName"),
            requestedScopes,
            GetRequiredValue(configuration, "ActorIdentifier"));

        var result = await bootstrapper.BootstrapAsync(
            request,
            cancellationToken);

        WriteResult(result);
    }



    private static void WriteResult(
        ManagedClientKeysBootstrapResult result)
    {
        Console.WriteLine(
            $"Organization ID: {result.OrganizationId}");

        Console.WriteLine(
            $"Client application ID: {result.ClientApplicationId}");

        Console.WriteLine(
            $"API-key credential ID: {result.ApiKeyCredentialId}");

        Console.WriteLine(
            $"Key ID: {result.KeyId}");

        switch (result)
        {
            case ManagedClientKeysBootstrapResult.Created created:
                Console.WriteLine(
                    "Bootstrap result: credential created.");

                Console.WriteLine(
                    "API key follows. It will not be displayed again:");

                Console.WriteLine(created.RawApiKey);
                break;

            case ManagedClientKeysBootstrapResult.AlreadyExists:
                Console.WriteLine(
                    "Bootstrap result: credential already exists.");

                Console.WriteLine(
                    "No API key was generated or displayed.");
                break;

            default:
                throw new InvalidOperationException(
                    "The bootstrap result type is unsupported.");
        }
    }



    private static string GetRequiredValue(
        IConfiguration configuration,
        string settingName)
    {
        var configurationPath =
            $"{SectionName}:{settingName}";

        var value = configuration[configurationPath];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{configurationPath} must be provided.");
        }

        return value;
    }
}
