namespace FileProcessingService.Api.Security.ManagedClientKeys.Provisioning;


internal static class ManagedClientKeysBootstrapRequestValidator
{
    private const int MaximumOrganizationCodeLength = 64;
    private const int MaximumClientIdentifierLength = 100;
    private const int MaximumDisplayNameLength = 200;
    private const int MaximumActorIdentifierLength = 200;
    private const int MaximumScopeLength = 100;


    private static readonly HashSet<string> SupportedScopes =
        new(StringComparer.Ordinal)
        {
            ApiKeyAuthorizationDefaults.ProcessFilesScope,
            ApiKeyAuthorizationDefaults.ReadReportsScope
        };



    public static NormalizedManagedClientKeysBootstrapRequest ValidateAndNormalize(ManagedClientKeysBootstrapRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Scopes);

        var organizationCode = ValidateIdentifier(
            request.OrganizationCode,
            nameof(request.OrganizationCode),
            MaximumOrganizationCodeLength);

        var organizationDisplayName = ValidateText(
            request.OrganizationDisplayName,
            nameof(request.OrganizationDisplayName),
            MaximumDisplayNameLength);

        var clientIdentifier = ValidateIdentifier(
                request.ClientIdentifier,
                nameof(request.ClientIdentifier),
                MaximumClientIdentifierLength)
            .ToLowerInvariant();

        var clientDisplayName = ValidateText(
            request.ClientDisplayName,
            nameof(request.ClientDisplayName),
            MaximumDisplayNameLength);

        var actorIdentifier = ValidateText(
            request.ActorIdentifier,
            nameof(request.ActorIdentifier),
            MaximumActorIdentifierLength);

        var scopes = ValidateScopes(request.Scopes);

        return new NormalizedManagedClientKeysBootstrapRequest(
            organizationCode,
            organizationCode.ToUpperInvariant(),
            organizationDisplayName,
            clientIdentifier,
            clientDisplayName,
            scopes,
            actorIdentifier);
    }



    private static string ValidateIdentifier(
        string value,
        string parameterName,
        int maximumLength)
    {
        var normalizedValue = ValidateText(
            value,
            parameterName,
            maximumLength);

        if (!normalizedValue.All(IsIdentifierCharacter))
        {
            throw new ArgumentException(
                "Identifiers may contain only ASCII letters, digits, hyphens, underscores, and periods.",
                parameterName);
        }

        return normalizedValue;
    }



    private static string ValidateText(
        string value,
        string parameterName,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A non-empty value is required.",
                parameterName);
        }

        var trimmedValue = value.Trim();

        if (trimmedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return trimmedValue;
    }



    private static string[] ValidateScopes(IReadOnlyCollection<string> requestedScopes)
    {
        if (requestedScopes.Count == 0)
        {
            throw new ArgumentException(
                "At least one API-key scope is required.",
                nameof(requestedScopes));
        }

        var normalizedScopes = new SortedSet<string>(
            StringComparer.Ordinal);

        foreach (var requestedScope in requestedScopes)
        {
            var scope = ValidateText(
                requestedScope,
                nameof(requestedScopes),
                MaximumScopeLength);

            if (!SupportedScopes.Contains(scope))
            {
                throw new ArgumentException(
                    $"Unsupported API-key scope: {scope}.",
                    nameof(requestedScopes));
            }

            normalizedScopes.Add(scope);
        }

        return normalizedScopes.ToArray();
    }



    private static bool IsIdentifierCharacter(char character) =>
        char.IsAsciiLetterOrDigit(character)
        || character is '-' or '_' or '.';
}
