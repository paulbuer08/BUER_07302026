using System.Diagnostics;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Security;


public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyCredentialValidator credentialValidator)
        : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string AuthenticationFailureMessage = "A valid API key is required.";
    private readonly IApiKeyCredentialValidator _credentialValidator = credentialValidator;


    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }
        if (headerValues.Count != 1)
        {
            return AuthenticateResult.Fail(AuthenticationFailureMessage);
        }

        var providedKey = headerValues[0];
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return AuthenticateResult.Fail(AuthenticationFailureMessage);
        }

        var validationResult = await _credentialValidator.ValidateAsync(providedKey, Context.RequestAborted);
        if (!validationResult.Succeeded
            || string.IsNullOrWhiteSpace(validationResult.ClientId))
        {
            return AuthenticateResult.Fail(AuthenticationFailureMessage);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, validationResult.ClientId),
            new(ApiKeyAuthorizationDefaults.ClientIdClaimType, validationResult.ClientId)
        };

        claims.AddRange(
            validationResult.Scopes
                .Distinct(StringComparer.Ordinal)
                .Select(scope => new Claim(
                    ApiKeyAuthorizationDefaults.ScopeClaimType,
                    scope)));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }



    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = Scheme.Name;

        await Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized",
            detail: AuthenticationFailureMessage,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = "invalid_api_key",
                ["traceId"] = Activity.Current?.Id ?? Context.TraceIdentifier
            }).ExecuteAsync(Context);
    }
}
