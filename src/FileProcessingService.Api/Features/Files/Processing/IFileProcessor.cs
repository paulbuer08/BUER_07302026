namespace FileProcessingService.Api.Features.Files.Processing;


public interface IFileProcessor
{
    string SupportedFileExtension { get; }
    Task<FileProcessingOutcome> ProcessAsync(Stream stream, CancellationToken cancellationToken);
}
