using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Configuration;


public sealed class FileUploadFormOptionsSetup : IConfigureOptions<FormOptions>
{
    private readonly FileProcessingOptions _fileProcessingOptions;

    public FileUploadFormOptionsSetup(IOptions<FileProcessingOptions> fileProcessingOptions)
    {
        _fileProcessingOptions = fileProcessingOptions.Value;
    }


    public void Configure(FormOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.MultipartBodyLengthLimit = _fileProcessingOptions.MaximumFileSizeBytes;
    }
}
