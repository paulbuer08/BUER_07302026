using Microsoft.AspNetCore.Authentication;

namespace FileProcessingService.Api.Security;


public sealed class ApiKeyAuthenticationSchemeOptions
    : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = "X-Api-Key";
}
