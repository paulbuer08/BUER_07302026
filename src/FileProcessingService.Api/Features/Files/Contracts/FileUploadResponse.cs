namespace FileProcessingService.Api.Features.Files.Contracts;


public sealed record FileUploadResponse(
    Guid ProcessingId,
    string FileName,
    string Format,
    long FileSizeBytes,
    int RecordsProcessed,
    long ProcessingDurationMilliseconds,
    object Result);
