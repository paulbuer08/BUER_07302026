using FileProcessingService.Api.Features.Files.Contracts;

namespace FileProcessingService.Api.Features.Files.Services;


public interface IFileProcessingOrchestrator
{
    Task<FileUploadResponse> ProcessAsync(
        IFormFile file,
        CancellationToken cancellationToken);
}
