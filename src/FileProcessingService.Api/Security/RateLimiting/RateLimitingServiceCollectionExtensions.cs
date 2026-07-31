using System.Globalization;
using System.Threading.RateLimiting;

namespace FileProcessingService.Api.Security.RateLimiting;


public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddFileProcessingRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredOptions = configuration
            .GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        services
            .AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.SectionName)
            .Validate(options => options.PermitLimit is >= 1 and <= 1_000, "PermitLimit must be between 1 and 1,000.")
            .Validate(options => options.WindowSeconds is >= 1 and <= 3_600, "WindowSeconds must be between 1 and 3,600.")
            .Validate(options => options.QueueLimit is >= 0 and <= 1_000, "QueueLimit must be between 0 and 1,000.")
            .ValidateOnStart();


        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(
                RateLimitingOptions.PolicyName,
                httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                AutoReplenishment = true,
                                PermitLimit = configuredOptions.PermitLimit,
                                Window = TimeSpan.FromSeconds(configuredOptions.WindowSeconds),
                                QueueLimit = configuredOptions.QueueLimit,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            }));

            options.OnRejected =
                async (context, _) =>
                {
                    var httpContext = context.HttpContext;

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        httpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                    }

                    await Results.Problem(
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Rate limit exceeded",
                        detail:
                            "Too many requests were received. " +
                            "Try again after the indicated delay.",
                        instance: httpContext.Request.Path,
                        extensions:
                            new Dictionary<string, object?>
                            {
                                ["errorCode"] = "rate_limit_exceeded"
                            })
                        .ExecuteAsync(httpContext);
                };
        });

        return services;
    }

}
