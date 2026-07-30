using System.Text.Json.Serialization;

namespace FileProcessingService.Api.Features.Files.Tracking;


[JsonConverter(typeof(JsonStringEnumConverter<FileProcessingStatus>))]
public enum FileProcessingStatus
{
    Succeeded,
    Failed
}
