using System.Net;
using System.Text;
using System.Text.Json;
using FileProcessingService.Api.Security;

namespace FileProcessingService.Api.Tests.Integration;


public sealed class FileValidationApiTests(FileProcessingWebApplicationFactory factory)
    : IClassFixture<FileProcessingWebApplicationFactory>
{
    private const string FilesEndpoint = "/api/v1/files";
    private readonly FileProcessingWebApplicationFactory _factory = factory;


    [Fact]
    public async Task DisabledJsonUploadReturnsUnsupportedMediaTypeAndIsTracked()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var isolatedFactory = new FileProcessingWebApplicationFactory();
        using var client = isolatedFactory.CreateClient();

        using var uploadResponse = await UploadAsync(
            client,
            new StringContent(
                """[{ "id": 1, "isActive": true, "amount": 100 }]""",
                Encoding.UTF8,
                "application/json"),
            "disabled.json",
            cancellationToken);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, uploadResponse.StatusCode);

        using var problemDocument = await ReadJsonAsync(uploadResponse, cancellationToken);
        Assert.Equal("json_support_disabled", problemDocument.RootElement.GetProperty("errorCode").GetString());

        using var reportRequest = CreateAuthenticatedRequest(HttpMethod.Get);
        using var reportResponse = await client.SendAsync(reportRequest, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);

        using var reportDocument = await ReadJsonAsync(reportResponse, cancellationToken);
        var report = reportDocument.RootElement;
        Assert.Equal(1, report.GetProperty("totalFilesReceived").GetInt64());
        Assert.Equal(0, report.GetProperty("totalFilesProcessedSuccessfully").GetInt64());
        Assert.Equal(1, report.GetProperty("totalFilesFailed").GetInt64());

        var failedRecord = Assert.Single(report.GetProperty("recentFiles").EnumerateArray().ToArray());
        Assert.Equal("disabled.json", failedRecord.GetProperty("fileName").GetString());
        Assert.Equal("Failed", failedRecord.GetProperty("status").GetString());
        Assert.Equal("json_support_disabled", failedRecord.GetProperty("errorCode").GetString());
    }



    [Fact]
    public async Task EmptyUploadReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        using var response = await UploadAsync(client, new ByteArrayContent([]), "empty.csv", cancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var problemDocument = await ReadJsonAsync(response, cancellationToken);
        Assert.Equal("file_empty", problemDocument.RootElement.GetProperty("errorCode").GetString());
    }



    [Fact]
    public async Task UnsupportedExtensionReturnsUnsupportedMediaType()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        using var response = await UploadAsync(client, new StringContent("This is not a supported file.", Encoding.UTF8, "text/plain"), "unsupported.txt", cancellationToken);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);

        using var problemDocument = await ReadJsonAsync(response, cancellationToken);
        Assert.Equal("file_format_unsupported", problemDocument.RootElement.GetProperty("errorCode").GetString());
    }



    [Fact]
    public async Task OversizedFileReturnsPayloadTooLarge()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        var oversizedBytes = new byte[FileProcessingWebApplicationFactory.TestMaximumFileSizeBytes + 1];
        using var response = await UploadAsync(client, new ByteArrayContent(oversizedBytes), "oversized.csv", cancellationToken);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);

        using var problemDocument = await ReadJsonAsync(response, cancellationToken);
        Assert.Equal("file_too_large", problemDocument.RootElement.GetProperty("errorCode").GetString());
    }



    [Fact]
    public async Task ReportContainsSuccessfulAndFailedCounters()
    {
        const string csv =
            """
            Id,Amount
            1,100
            2,300
            """;

        var cancellationToken = TestContext.Current.CancellationToken;
        using var isolatedFactory = new FileProcessingWebApplicationFactory();
        using var client = isolatedFactory.CreateClient();

        using var successfulResponse = await UploadAsync(client, new StringContent(csv, Encoding.UTF8, "text/csv"), "successful.csv", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, successfulResponse.StatusCode);

        using var failedResponse = await UploadAsync(client, new StringContent("Unsupported content", Encoding.UTF8, "text/plain"), "failed.txt", cancellationToken);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, failedResponse.StatusCode);

        using var reportRequest = CreateAuthenticatedRequest(HttpMethod.Get);
        using var reportResponse = await client.SendAsync(reportRequest, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);

        using var reportDocument = await ReadJsonAsync(reportResponse, cancellationToken);
        var report = reportDocument.RootElement;
        Assert.Equal(2, report.GetProperty("totalFilesReceived").GetInt64());
        Assert.Equal(1, report.GetProperty("totalFilesProcessedSuccessfully").GetInt64());
        Assert.Equal(1, report.GetProperty("totalFilesFailed").GetInt64());
        Assert.Equal(2, report.GetProperty("recentFiles").GetArrayLength());
    }



    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, HttpContent fileContent, string fileName, CancellationToken cancellationToken)
    {
        using var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(fileContent, "File", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, FilesEndpoint)
        {
            Content = multipartContent
        };

        AddApiKey(request);
        return await client.SendAsync(request, cancellationToken);
    }



    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method)
    {
        var request = new HttpRequestMessage(method, FilesEndpoint);
        AddApiKey(request);
        return request;
    }



    private static void AddApiKey(HttpRequestMessage request)
    {
        request.Headers.Add(ApiKeyAuthenticationDefaults.HeaderName, FileProcessingWebApplicationFactory.TestApiKey);
    }



    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonDocument.Parse(json);
    }

}
