using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FileProcessingService.Api.Tests.Integration;


public sealed class FileProcessingWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestApiKey = "Integration-Test-Api-Key-2026-1234567890!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var testSettings =
                new Dictionary<string, string?>
                {
                    ["ApiKeyAuthentication:ApiKey"] = TestApiKey,
                    ["Swagger:IsEnabled"] = "false",
                    ["FileProcessing:CsvSupport:IsEnabled"] = "true",
                    ["FileProcessing:JsonSupport:IsEnabled"] = "false",
                    ["Logging:LogLevel:Default"] = "Warning"
                };

            configuration.AddInMemoryCollection(testSettings);
        });
    }

}
