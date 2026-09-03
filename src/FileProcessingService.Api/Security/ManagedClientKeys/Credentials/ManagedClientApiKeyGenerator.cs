using System.Security.Cryptography;

namespace FileProcessingService.Api.Security.ManagedClientKeys.Credentials;


internal static class ManagedClientApiKeyGenerator
{
    private const int SecretByteLength = 32;


    public static GeneratedManagedClientApiKey Generate()
    {
        var keyId = Guid.NewGuid().ToString("N");
        Span<byte> secretBytes = stackalloc byte[SecretByteLength];
        RandomNumberGenerator.Fill(secretBytes);

        try
        {
            var secret = Convert
                .ToBase64String(secretBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            var rawApiKey = ManagedClientApiKeyFormat.Create(
                keyId,
                secret);

            var secretHash = ManagedClientApiKeyHasher.ComputeHash(
                secret);

            return new GeneratedManagedClientApiKey(
                keyId,
                rawApiKey,
                secretHash,
                ManagedClientApiKeyHasher.AlgorithmVersion);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secretBytes);
        }
    }
}
