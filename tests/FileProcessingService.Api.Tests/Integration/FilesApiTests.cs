using System.Net;
using System.Text;
using System.Text.Json;
using FileProcessingService.Api.Security;

namespace FileProcessingService.Api.Tests.Integration;


public sealed class FilesApiTests(FileProcessingWebApplicationFactory factory)
    : IClassFixture<FileProcessingWebApplicationFactory>
{
    private readonly FileProcessingWebApplicationFactory _factory = factory;


    [Fact]
    public async Task GetReportWithoutApiKeyReturnsUnauthorized()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/files");
        using var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }



    [Fact]
    public async Task GetReportWithInvalidApiKeyReturnsUnauthorized()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/files");

        AddApiKey(request, "incorrect-api-key");
        using var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }



    [Fact]
    public async Task CsvUploadReturnsAverageAndAppearsInReport()
    {
        const string csv =
            """
            Id,Name,Amount
            1,Alpha,100.00
            2,Beta,200.00
            3,Gamma,300.00
            """;

        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        using var fileContent = new StringContent(csv, Encoding.UTF8, "text/csv");
        using var multipartContent = new MultipartFormDataContent();

        multipartContent.Add(fileContent, "File", "integration.csv");
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/files")
        {
            Content = multipartContent
        };

        AddApiKey(uploadRequest, FileProcessingWebApplicationFactory.TestApiKey);
        using var uploadResponse = await client.SendAsync(uploadRequest, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        var uploadJson = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
        using var uploadDocument = JsonDocument.Parse(uploadJson);
        var uploadRoot = uploadDocument.RootElement;
        var processingId = uploadRoot.GetProperty("processingId").GetGuid();

        Assert.Equal("integration.csv", uploadRoot.GetProperty("fileName").GetString());
        Assert.Equal("csv", uploadRoot.GetProperty("format").GetString());
        Assert.Equal(3, uploadRoot.GetProperty("recordsProcessed").GetInt32());
        Assert.Equal(200m, uploadRoot.GetProperty("result").GetProperty("average").GetDecimal());

        using var reportRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/files");
        AddApiKey(reportRequest, FileProcessingWebApplicationFactory.TestApiKey);
        using var reportResponse = await client.SendAsync(reportRequest, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);

        var reportJson = await reportResponse.Content.ReadAsStringAsync(cancellationToken);
        using var reportDocument = JsonDocument.Parse(reportJson);
        var recentFiles = reportDocument.RootElement.GetProperty("recentFiles").EnumerateArray().ToArray();

        Assert.Contains(
            recentFiles,
            record => record.GetProperty("processingId").GetGuid() == processingId);
    }



    private static void AddApiKey(HttpRequestMessage request, string apiKey)
    {
        request.Headers.Add(ApiKeyAuthenticationDefaults.HeaderName, apiKey);
    }

}
