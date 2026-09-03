using System.Security.Cryptography;
using FileProcessingService.Api.Security.ManagedClientKeys.Domain;
using FileProcessingService.Api.Security.ManagedClientKeys.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Security.ManagedClientKeys.Credentials;


internal sealed class ManagedClientKeyCredentialValidator(
    ManagedClientKeysDbContext dbContext,
    IOptions<AuthenticationOptions> authenticationOptions,
    TimeProvider timeProvider)
        : IApiKeyCredentialValidator
{
    private readonly ManagedClientKeysDbContext _dbContext = dbContext;
    private readonly ManagedClientKeysSettings _settings = authenticationOptions.Value.ManagedClientKeysSettings;
    private readonly TimeProvider _timeProvider = timeProvider;



    public async ValueTask<ApiKeyValidationResult> ValidateAsync(string providedKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ManagedClientApiKeyFormat.TryParse(providedKey, out var parsedKey))
        {
            return ApiKeyValidationResult.Failed;
        }

        var credential = await _dbContext.ApiKeyCredentials
            .AsNoTracking()
            .Where(candidate => candidate.KeyId == parsedKey.KeyId)
            .Select(candidate => new
            {
                candidate.SecretHash,
                candidate.HashAlgorithmVersion,
                candidate.NotBeforeUtc,
                candidate.ExpiresAtUtc,
                candidate.RevokedAtUtc,

                ClientIdentifier = candidate.ClientApplication.ClientIdentifier,
                ClientStatus = candidate.ClientApplication.Status,
                OrganizationStatus = candidate.ClientApplication.Organization.Status,
                Scopes = candidate.Scopes
                    .Select(scope => scope.Scope)
                    .OrderBy(scope => scope)
                    .ToArray()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (credential is null
            || credential.HashAlgorithmVersion != ManagedClientApiKeyHasher.AlgorithmVersion)
        {
            return ApiKeyValidationResult.Failed;
        }

        bool validSecret;
        try
        {
            validSecret = ManagedClientApiKeyHasher.Verify(
                parsedKey.Secret.Span,
                credential.SecretHash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(credential.SecretHash);
        }

        if (!validSecret)
        {
            return ApiKeyValidationResult.Failed;
        }


        var now = _timeProvider.GetUtcNow();
        if (credential.OrganizationStatus != OrganizationStatus.Active
            || credential.ClientStatus != ClientApplicationStatus.Active
            || credential.RevokedAtUtc is not null
            || now < credential.NotBeforeUtc
            || credential.ExpiresAtUtc is { } expiration
                && now >= expiration
            || _settings.RequireExpiration
                && credential.ExpiresAtUtc is null)
        {
            return ApiKeyValidationResult.Failed;
        }

        return ApiKeyValidationResult.Success(
            credential.ClientIdentifier,
            credential.Scopes);
    }
}
