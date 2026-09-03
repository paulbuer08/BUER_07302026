using System.Security.Cryptography;
using System.Text;

namespace FileProcessingService.Api.Security.ManagedClientKeys.Credentials;


internal static class ManagedClientApiKeyHasher
{
    public const short AlgorithmVersion = 1;
    public const int HashLength = 32;


    public static byte[] ComputeHash(ReadOnlySpan<char> secret)
    {
        if (!ManagedClientApiKeyFormat.IsValidSecret(secret))
        {
            throw new ArgumentException("The managed API-key secret has an invalid format.", nameof(secret));
        }

        Span<byte> secretBytes = stackalloc byte[ManagedClientApiKeyFormat.SecretLength];
        try
        {
            _ = Encoding.ASCII.GetBytes(secret, secretBytes);
            return SHA256.HashData(secretBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secretBytes);
        }
    }



    public static bool Verify(ReadOnlySpan<char> providedSecret, ReadOnlySpan<byte> expectedHash)
    {
        if (!ManagedClientApiKeyFormat.IsValidSecret(providedSecret)
            || expectedHash.Length != HashLength)
        {
            return false;
        }

        Span<byte> secretBytes = stackalloc byte[ManagedClientApiKeyFormat.SecretLength];
        Span<byte> providedHash = stackalloc byte[HashLength];
        try
        {
            _ = Encoding.ASCII.GetBytes(providedSecret, secretBytes);
            _ = SHA256.HashData(secretBytes, providedHash);

            return CryptographicOperations.FixedTimeEquals(
                providedHash,
                expectedHash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secretBytes);
            CryptographicOperations.ZeroMemory(providedHash);
        }
    }

}
