namespace FileProcessingService.Api.Security.ManagedClientKeys.Provisioning;

public interface IManagedClientKeysBootstrapper
{
    Task<ManagedClientKeysBootstrapResult> BootstrapAsync(
        ManagedClientKeysBootstrapRequest request,
        CancellationToken cancellationToken = default);
}
