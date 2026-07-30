using System.ComponentModel.DataAnnotations;

namespace FileProcessingService.Api.Features.Files.Contracts;


public sealed class UploadFileRequest
{
    [Required]
    public IFormFile File { get; init; } = null!;
}
