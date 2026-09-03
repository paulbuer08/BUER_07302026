namespace FileProcessingService.Api.Security.ManagedClientKeys.Domain;

public enum ApiKeyLifecycleEventType
{
    Issued,
    Activated,
    RotationStarted,
    Revoked
}
