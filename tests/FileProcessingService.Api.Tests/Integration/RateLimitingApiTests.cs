using System.Net;
using System.Text.Json;
using FileProcessingService.Api.Security;

namespace FileProcessingService.Api.Tests.Integration;


public sealed class RateLimitingApiTests
{
    private const string FilesEndpoint = "/api/v1/files";
    private const int ConfiguredPermitLimit = 20;


    [Fact]
    public async Task RequestsBeyondConfiguredLimitReturnTooManyRequests()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new FileProcessingWebApplicationFactory();
        using var client = factory.CreateClient();

        for (var requestNumber = 1; requestNumber <= ConfiguredPermitLimit; requestNumber++)
        {
            using var allowedRequest = CreateAuthenticatedRequest();
            using var allowedResponse = await client.SendAsync(allowedRequest, cancellationToken);
            Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        }

        using var rejectedRequest = CreateAuthenticatedRequest();
        using var rejectedResponse = await client.SendAsync(rejectedRequest, cancellationToken);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        Assert.Equal("application/problem+json", rejectedResponse.Content.Headers.ContentType?.MediaType);

        var json = await rejectedResponse.Content.ReadAsStringAsync(cancellationToken);
        using var problemDocument = JsonDocument.Parse(json);
        Assert.Equal("rate_limit_exceeded", problemDocument.RootElement.GetProperty("errorCode").GetString());
    }



    private static HttpRequestMessage CreateAuthenticatedRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, FilesEndpoint);
        request.Headers.Add(
            ApiKeyAuthenticationDefaults.HeaderName,
            FileProcessingWebApplicationFactory.TestApiKey);

        return request;
    }

}
