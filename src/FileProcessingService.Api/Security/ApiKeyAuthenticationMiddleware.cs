using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Security;


public sealed class ApiKeyAuthenticationMiddleware(RequestDelegate next, IOptions<ApiKeyAuthenticationOptions> options)
{
    private readonly RequestDelegate _next = next;
    private readonly byte[] _expectedKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.ApiKey));


    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresAuthentication(context.Request.Path))
        {
            await _next(context);
            return;
        }
        if (!TryGetApiKey(context, out var providedKey) || !IsValidApiKey(providedKey))
        {
            await WriteUnauthorizedResponseAsync(context);
            return;
        }

        await _next(context);
    }



    private static bool RequiresAuthentication(PathString path) =>
        path.StartsWithSegments(
            ApiKeyAuthenticationDefaults.ProtectedPathPrefix,
            StringComparison.OrdinalIgnoreCase);



    private static bool TryGetApiKey(HttpContext context, out string providedKey)
    {
        providedKey = string.Empty;

        if (!context.Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var values)
            || values.Count != 1)
        {
            return false;
        }

        var candidate = values[0];
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        providedKey = candidate;
        return true;
    }



    private bool IsValidApiKey(string providedKey)
    {
        var providedKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedKey));

        return CryptographicOperations.FixedTimeEquals(
            _expectedKeyHash,
            providedKeyHash);
    }



    private static async Task WriteUnauthorizedResponseAsync(HttpContext context)
    {
        context.Response.Headers.WWWAuthenticate = "ApiKey";

        await Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized",
            detail: "A valid API key is required.",
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = "invalid_api_key",
                ["traceId"] =
                    Activity.Current?.Id
                    ?? context.TraceIdentifier
            }).ExecuteAsync(context);
    }

}
