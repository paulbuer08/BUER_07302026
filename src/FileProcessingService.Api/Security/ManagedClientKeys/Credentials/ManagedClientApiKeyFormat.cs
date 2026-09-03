namespace FileProcessingService.Api.Security.ManagedClientKeys.Credentials;


internal static class ManagedClientApiKeyFormat
{
    private const string Prefix = "fpk1_";
    private const char Separator = '.';
    private const int KeyIdLength = 32;
    internal const int SecretLength = 43;

    private static readonly int SeparatorIndex = Prefix.Length + KeyIdLength;
    private static readonly int SecretStartIndex = SeparatorIndex + 1;
    private static readonly int ExpectedLength = SecretStartIndex + SecretLength;



    public static bool TryParse(string providedKey, out ParsedManagedClientApiKey parsedKey)
    {
        ArgumentNullException.ThrowIfNull(providedKey);
        parsedKey = default;

        if (providedKey.Length != ExpectedLength
            || !providedKey.StartsWith(Prefix, StringComparison.Ordinal)
            || providedKey[SeparatorIndex] != Separator)
        {
            return false;
        }

        var keyId = providedKey.AsSpan(Prefix.Length, KeyIdLength);
        if (!IsValidKeyId(keyId))
        {
            return false;
        }

        var secret = providedKey.AsSpan(SecretStartIndex, SecretLength);
        if (!IsValidSecret(secret))
        {
            return false;
        }

        parsedKey = new ParsedManagedClientApiKey(
            keyId.ToString(),
            providedKey.AsMemory(SecretStartIndex, SecretLength));

        return true;
    }



    private static bool IsValidKeyId(ReadOnlySpan<char> keyId)
    {
        foreach (var character in keyId)
        {
            if (!IsLowercaseHexadecimal(character))
            {
                return false;
            }
        }
        return true;
    }



    internal static bool IsValidSecret(ReadOnlySpan<char> secret)
    {
        if (secret.Length != SecretLength)
        {
            return false;
        }

        foreach (var character in secret)
        {
            if (!IsBase64UrlCharacter(character))
            {
                return false;
            }
        }

        return true;
    }



    private static bool IsLowercaseHexadecimal(char character) =>
        character is >= '0' and <= '9'
        or >= 'a' and <= 'f';



    private static bool IsBase64UrlCharacter(char character) =>
        char.IsAsciiLetterOrDigit(character)
        || character is '-' or '_';
}
