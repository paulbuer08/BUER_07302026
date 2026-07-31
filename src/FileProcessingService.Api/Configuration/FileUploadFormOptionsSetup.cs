using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Configuration;


public sealed class FileUploadFormOptionsSetup(IOptions<FileProcessingOptions> fileProcessingOptions)
    : IConfigureOptions<FormOptions>
{
    private const long MultipartOverheadAllowanceBytes = 64 * 1024;
    private readonly FileProcessingOptions _fileProcessingOptions = fileProcessingOptions.Value;


    public void Configure(FormOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.MultipartBodyLengthLimit = checked(
            _fileProcessingOptions.MaximumFileSizeBytes
            + MultipartOverheadAllowanceBytes);
    }
}
