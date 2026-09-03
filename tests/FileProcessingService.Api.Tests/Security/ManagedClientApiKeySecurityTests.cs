using System.Security.Cryptography;
using FileProcessingService.Api.Security.ManagedClientKeys.Credentials;

namespace FileProcessingService.Api.Tests.Security;


public sealed class ManagedClientApiKeySecurityTests
{
    private const string ValidKeyId = "0123456789abcdef0123456789abcdef";
    private static readonly string ValidSecret = new('A', 43);


    [Fact]
    public void ValidManagedClientApiKeyCanBeParsed()
    {
        var providedKey = $"fpk1_{ValidKeyId}.{ValidSecret}";
        var succeeded = ManagedClientApiKeyFormat.TryParse(
            providedKey,
            out var parsedKey);

        Assert.True(succeeded);
        Assert.Equal(ValidKeyId, parsedKey.KeyId);
        Assert.Equal(ValidSecret, parsedKey.Secret.ToString());
    }



    [Theory]
    [MemberData(nameof(InvalidManagedClientApiKeys))]
    public void InvalidManagedClientApiKeyCannotBeParsed(string providedKey)
    {
        var succeeded = ManagedClientApiKeyFormat.TryParse(
            providedKey,
            out _);

        Assert.False(succeeded);
    }



    [Theory]
    [InlineData(42)]
    [InlineData(44)]
    public void SecretWithInvalidLengthCannotBeHashed(
    int secretLength)
    {
        var invalidSecret = new string('A', secretLength);

        Assert.Throws<ArgumentException>(() =>
            ManagedClientApiKeyHasher.ComputeHash(invalidSecret.AsSpan())
        );
    }



    [Fact]
    public void ComputedHashHasExpectedLengthAndCanBeVerified()
    {
        var hash = ManagedClientApiKeyHasher.ComputeHash(ValidSecret.AsSpan());
        try
        {
            Assert.Equal(ManagedClientApiKeyHasher.HashLength, hash.Length);
            Assert.True(
                ManagedClientApiKeyHasher.Verify(
                    ValidSecret.AsSpan(),
                    hash));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hash);
        }
    }



    [Fact]
    public void DifferentSecretDoesNotMatchStoredHash()
    {
        var hash = ManagedClientApiKeyHasher.ComputeHash(ValidSecret.AsSpan());
        try
        {
            var differentSecret = new string('B', 43);
            Assert.False(
                ManagedClientApiKeyHasher.Verify(
                    differentSecret.AsSpan(),
                    hash));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hash);
        }
    }



    public static TheoryData<string> InvalidManagedClientApiKeys
    {
        get
        {
            var uppercaseKeyId = $"F{ValidKeyId[1..]}";

            return new TheoryData<string>
            {
                "invalid",
                $"fpk2_{ValidKeyId}.{ValidSecret}",
                $"fpk1_{uppercaseKeyId}.{ValidSecret}",
                $"fpk1_{ValidKeyId}:{ValidSecret}",
                $"fpk1_{ValidKeyId}.{new string('+', 43)}",
                $"fpk1_{ValidKeyId}.{ValidSecret}A"
            };
        }
    }

}
