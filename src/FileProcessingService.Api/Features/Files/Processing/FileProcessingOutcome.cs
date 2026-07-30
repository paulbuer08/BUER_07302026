namespace FileProcessingService.Api.Features.Files.Processing;


public sealed record FileProcessingOutcome(
    string Format,
    int RecordsProcessed,
    object Result);
