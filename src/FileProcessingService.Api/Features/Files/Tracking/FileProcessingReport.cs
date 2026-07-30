namespace FileProcessingService.Api.Features.Files.Tracking;


public sealed record FileProcessingReport(
    long TotalFilesReceived,
    long TotalFilesProcessedSuccessfully,
    long TotalFilesFailed,
    IReadOnlyList<FileProcessingRecord> RecentFiles);
