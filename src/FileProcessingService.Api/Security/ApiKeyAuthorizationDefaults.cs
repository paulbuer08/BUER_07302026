namespace FileProcessingService.Api.Security;

public static class ApiKeyAuthorizationDefaults
{
    public const string ClientIdClaimType = "client_id";
    public const string ScopeClaimType = "scope";

    public const string ProcessFilesScope = "files:process";
    public const string ReadReportsScope = "files:report";

    public const string ProcessFilesPolicy = "ProcessFiles";
    public const string ReadReportsPolicy = "ReadReports";

    public const string SharedKeyClientId = "shared-key-client";
}
