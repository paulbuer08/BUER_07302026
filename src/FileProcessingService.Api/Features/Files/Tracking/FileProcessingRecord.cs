namespace FileProcessingService.Api.Features.Files.Tracking;


public sealed record FileProcessingRecord(
    Guid ProcessingId,
    string FileName,
    string Format,
    long FileSizeBytes,
    FileProcessingStatus Status,
    int? RecordsProcessed,
    long ProcessingDurationMilliseconds,
    DateTimeOffset CompletedAtUtc,
    string? ErrorCode);
