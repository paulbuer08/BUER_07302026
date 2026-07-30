namespace FileProcessingService.Api.Features.Files.Processing;


public sealed record CsvAverageResult(
    string ColumnName,
    decimal Average);
