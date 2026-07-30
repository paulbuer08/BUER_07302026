using System.Text.Json;

namespace FileProcessingService.Api.Features.Files.Processing;


public sealed record JsonFilterResult(
    int RecordsMatched,
    IReadOnlyList<JsonElement> Records);
