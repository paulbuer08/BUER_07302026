namespace FileProcessingService.Api.Security;

public interface IApiKeyCredentialValidator
{
    ValueTask<ApiKeyValidationResult> ValidateAsync(string providedKey, CancellationToken cancellationToken);
}
