using System.Security.Cryptography;
using FileProcessingService.Api.Security.ManagedClientKeys.Credentials;
using FileProcessingService.Api.Security.ManagedClientKeys.Domain;
using FileProcessingService.Api.Security.ManagedClientKeys.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FileProcessingService.Api.Security.ManagedClientKeys.Provisioning;


internal sealed class ManagedClientKeysBootstrapper(
    ManagedClientKeysDbContext dbContext,
    IOptions<AuthenticationOptions> authenticationOptions,
    TimeProvider timeProvider)
        : IManagedClientKeysBootstrapper
{
    private readonly ManagedClientKeysSettings _settings = authenticationOptions.Value.ManagedClientKeysSettings;


    public async Task<ManagedClientKeysBootstrapResult> BootstrapAsync(
        ManagedClientKeysBootstrapRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest =
            ManagedClientKeysBootstrapRequestValidator
                .ValidateAndNormalize(request);

        var now = timeProvider.GetUtcNow();

        var client = await dbContext.ClientApplications
            .Include(candidate => candidate.Organization)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.ClientIdentifier
                    == normalizedRequest.ClientIdentifier,
                cancellationToken);

        if (client is not null)
        {
            EnsureClientBelongsToOrganization(client, normalizedRequest.NormalizedOrganizationCode);
            EnsureOrganizationIsActive(client.Organization);
            EnsureClientIsActive(client);

            var existingCredential =
                await FindExistingCredentialAsync(
                    client.Id,
                    now,
                    cancellationToken);

            if (existingCredential is not null)
            {
                return new ManagedClientKeysBootstrapResult.AlreadyExists(
                    client.OrganizationId,
                    client.Id,
                    existingCredential.Id,
                    existingCredential.KeyId);
            }
        }

        var organization = client?.Organization;

        if (organization is null)
        {
            organization = await dbContext.Organizations
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.NormalizedCode
                        == normalizedRequest.NormalizedOrganizationCode,
                    cancellationToken);

            if (organization is null)
            {
                organization = CreateOrganization(
                    normalizedRequest,
                    now);

                dbContext.Organizations.Add(organization);
            }
            else
            {
                EnsureOrganizationIsActive(organization);
            }
        }

        if (client is null)
        {
            client = CreateClientApplication(
                organization,
                normalizedRequest,
                now);

            dbContext.ClientApplications.Add(client);
        }

        var generatedKey = ManagedClientApiKeyGenerator.Generate();

        try
        {
            var credential = CreateCredential(
                client,
                normalizedRequest,
                generatedKey,
                now);

            dbContext.ApiKeyCredentials.Add(credential);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
                when (IsUniqueConstraintViolation(exception))
            {
                dbContext.ChangeTracker.Clear();

                var concurrentResult =
                    await FindConcurrentBootstrapResultAsync(
                        normalizedRequest,
                        now,
                        cancellationToken);

                if (concurrentResult is not null)
                {
                    return concurrentResult;
                }

                throw;
            }

            dbContext.ChangeTracker.Clear();

            return new ManagedClientKeysBootstrapResult.Created(
                organization.Id,
                client.Id,
                credential.Id,
                credential.KeyId,
                generatedKey.RawApiKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                generatedKey.SecretHash);
        }
    }



    private async Task<ApiKeyCredential?> FindExistingCredentialAsync(
        Guid clientApplicationId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return await dbContext.ApiKeyCredentials
            .AsNoTracking()
            .Where(credential =>
                credential.ClientApplicationId == clientApplicationId
                && credential.RevokedAtUtc == null
                && (!_settings.RequireExpiration
                    || credential.ExpiresAtUtc != null)
                && (credential.ExpiresAtUtc == null
                    || credential.ExpiresAtUtc > now))
            .OrderByDescending(credential => credential.IssuedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }



    private async Task<ManagedClientKeysBootstrapResult?>
        FindConcurrentBootstrapResultAsync(
            NormalizedManagedClientKeysBootstrapRequest request,
            DateTimeOffset now,
            CancellationToken cancellationToken)
    {
        var client = await dbContext.ClientApplications
            .AsNoTracking()
            .Include(candidate => candidate.Organization)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.ClientIdentifier
                    == request.ClientIdentifier,
                cancellationToken);

        if (client is null
            || !string.Equals(
                client.Organization.NormalizedCode,
                request.NormalizedOrganizationCode,
                StringComparison.Ordinal))
        {
            return null;
        }

        var credential = await FindExistingCredentialAsync(
            client.Id,
            now,
            cancellationToken);

        return credential is null
            ? null
            : new ManagedClientKeysBootstrapResult.AlreadyExists(
                client.OrganizationId,
                client.Id,
                credential.Id,
                credential.KeyId);
    }



    private static Organization CreateOrganization(
        NormalizedManagedClientKeysBootstrapRequest request,
        DateTimeOffset now)
    {
        return new Organization
        {
            Id = Guid.NewGuid(),
            Code = request.OrganizationCode,
            NormalizedCode = request.NormalizedOrganizationCode,
            DisplayName = request.OrganizationDisplayName,
            Status = OrganizationStatus.Active,
            CreatedAtUtc = now,
            ApprovedAtUtc = now
        };
    }



    private static ClientApplication CreateClientApplication(
        Organization organization,
        NormalizedManagedClientKeysBootstrapRequest request,
        DateTimeOffset now)
    {
        return new ClientApplication
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            ClientIdentifier = request.ClientIdentifier,
            DisplayName = request.ClientDisplayName,
            Status = ClientApplicationStatus.Active,
            CreatedAtUtc = now,
            ActivatedAtUtc = now,
            Organization = organization
        };
    }



    private ApiKeyCredential CreateCredential(
        ClientApplication client,
        NormalizedManagedClientKeysBootstrapRequest request,
        GeneratedManagedClientApiKey generatedKey,
        DateTimeOffset now)
    {
        var credential = new ApiKeyCredential
        {
            Id = Guid.NewGuid(),
            ClientApplicationId = client.Id,
            KeyId = generatedKey.KeyId,
            SecretHash = generatedKey.SecretHash,
            HashAlgorithmVersion = generatedKey.HashAlgorithmVersion,
            IssuedAtUtc = now,
            NotBeforeUtc = now,
            ExpiresAtUtc = now.AddDays(
                _settings.DefaultLifetimeDays),
            ClientApplication = client
        };

        foreach (var scope in request.Scopes)
        {
            credential.Scopes.Add(
                new ApiKeyCredentialScope
                {
                    ApiKeyCredentialId = credential.Id,
                    Scope = scope,
                    ApiKeyCredential = credential
                });
        }

        credential.LifecycleEvents.Add(
            CreateLifecycleEvent(
                credential,
                ApiKeyLifecycleEventType.Issued,
                request.ActorIdentifier,
                "Initial managed-client credential issued.",
                now));

        credential.LifecycleEvents.Add(
            CreateLifecycleEvent(
                credential,
                ApiKeyLifecycleEventType.Activated,
                request.ActorIdentifier,
                "Initial managed-client credential activated.",
                now));

        return credential;
    }



    private static ApiKeyLifecycleEvent CreateLifecycleEvent(
        ApiKeyCredential credential,
        ApiKeyLifecycleEventType eventType,
        string actorIdentifier,
        string reason,
        DateTimeOffset now)
    {
        return new ApiKeyLifecycleEvent
        {
            Id = Guid.NewGuid(),
            ApiKeyCredentialId = credential.Id,
            EventType = eventType,
            OccurredAtUtc = now,
            ActorIdentifier = actorIdentifier,
            Reason = reason,
            ApiKeyCredential = credential
        };
    }



    private static void EnsureClientBelongsToOrganization(
        ClientApplication client,
        string normalizedOrganizationCode)
    {
        if (!string.Equals(
                client.Organization.NormalizedCode,
                normalizedOrganizationCode,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The client identifier is already assigned to another organization.");
        }
    }



    private static void EnsureOrganizationIsActive(
        Organization organization)
    {
        if (organization.Status != OrganizationStatus.Active)
        {
            throw new InvalidOperationException(
                "The existing organization is not active.");
        }
    }



    private static void EnsureClientIsActive(
        ClientApplication client)
    {
        if (client.Status != ClientApplicationStatus.Active)
        {
            throw new InvalidOperationException(
                "The existing client application is not active.");
        }
    }



    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
    }
}
