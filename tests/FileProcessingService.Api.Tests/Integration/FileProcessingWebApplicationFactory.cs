using FileProcessingService.Api.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FileProcessingService.Api.Tests.Integration;


public sealed class FileProcessingWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestApiKey = "Integration-Test-Api-Key-2026-1234567890!";
    public const int TestMaximumFileSizeBytes = 1_024;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var testSettings =
                new Dictionary<string, string?>
                {
                    [$"{AuthenticationOptions.SectionName}:{nameof(AuthenticationOptions.Mode)}"] = nameof(AuthenticationMode.SharedKey),
                    [$"{AuthenticationOptions.SectionName}:{nameof(AuthenticationOptions.HeaderName)}"] = ApiKeyAuthenticationDefaults.HeaderName,
                    [$"{AuthenticationOptions.SectionName}:{nameof(AuthenticationOptions.SharedKey)}"] = TestApiKey,
                    ["Swagger:IsEnabled"] = "false",
                    ["FileProcessing:MaximumFileSizeBytes"] = TestMaximumFileSizeBytes.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["FileProcessing:CsvSupport:IsEnabled"] = "true",
                    ["FileProcessing:JsonSupport:IsEnabled"] = "false",
                    ["Logging:LogLevel:Default"] = "Warning"
                };

            configuration.AddInMemoryCollection(testSettings);
        });
    }

}
