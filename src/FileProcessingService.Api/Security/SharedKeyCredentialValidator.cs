using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Security;


public sealed class SharedKeyCredentialValidator(
    IOptions<AuthenticationOptions> options)
    : IApiKeyCredentialValidator
{
    private readonly byte[] _expectedKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.SharedKey));


    public ValueTask<ApiKeyValidationResult> ValidateAsync(string providedKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var providedKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedKey));

        try
        {
            var isValid = CryptographicOperations.FixedTimeEquals(_expectedKeyHash, providedKeyHash);

            var result = isValid
                ? ApiKeyValidationResult.Success(
                    ApiKeyAuthorizationDefaults.SharedKeyClientId,
                    ApiKeyAuthorizationDefaults.ProcessFilesScope,
                    ApiKeyAuthorizationDefaults.ReadReportsScope)
                : ApiKeyValidationResult.Failed;

            return ValueTask.FromResult(result);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(providedKeyHash);
        }
    }
}
