using System.Diagnostics;

using FileProcessingService.Api.Configuration;
using FileProcessingService.Api.Features.Files.Contracts;
using FileProcessingService.Api.Features.Files.Processing;
using FileProcessingService.Api.Features.Files.Tracking;

using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Features.Files.Services;


public sealed class FileProcessingOrchestrator : IFileProcessingOrchestrator
{
    private readonly Dictionary<string, IFileProcessor> _processors;
    private readonly IFileProcessingTracker _tracker;
    private readonly FileProcessingOptions _options;


    public FileProcessingOrchestrator(
        IEnumerable<IFileProcessor> processors,
        IFileProcessingTracker tracker,
        IOptions<FileProcessingOptions> options)
    {
        _processors = processors.ToDictionary(processor => processor.SupportedFileExtension, StringComparer.OrdinalIgnoreCase);
        _tracker = tracker;
        _options = options.Value;
    }



    public async Task<FileUploadResponse> ProcessAsync(IFormFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        var processingId = Guid.NewGuid();
        var safeFileName = SanitizeFileName(file.FileName);
        var extension = Path.GetExtension(safeFileName);
        var format = GetFormat(extension);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            ValidateFile(file, safeFileName);
            var processor = GetProcessor(extension);

            await using var stream = file.OpenReadStream();
            var outcome = await processor.ProcessAsync(stream, cancellationToken);
            stopwatch.Stop();

            RecordSuccess(processingId, safeFileName, file.Length, outcome, stopwatch.ElapsedMilliseconds);

            return new FileUploadResponse(
                processingId,
                safeFileName,
                outcome.Format,
                file.Length,
                outcome.RecordsProcessed,
                stopwatch.ElapsedMilliseconds,
                outcome.Result);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            RecordFailure(
                processingId,
                safeFileName,
                format,
                file.Length,
                stopwatch.ElapsedMilliseconds,
                "request_cancelled");

            throw;
        }
        catch (FileProcessingException exception)
        {
            stopwatch.Stop();

            RecordFailure(
                processingId,
                safeFileName,
                format,
                file.Length,
                stopwatch.ElapsedMilliseconds,
                exception.ErrorCode);

            throw;
        }
    }



    private void ValidateFile(IFormFile file, string safeFileName)
    {
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new FileProcessingException(
                "file_name_missing",
                "The uploaded file must have a filename.");
        }
        if (file.Length == 0)
        {
            throw new FileProcessingException(
                "file_empty",
                "The uploaded file is empty.");
        }
        if (file.Length > _options.MaximumFileSizeBytes)
        {
            throw new FileProcessingException(
                "file_too_large",
                $"The uploaded file exceeds the maximum size of " +
                $"{_options.MaximumFileSizeBytes} bytes.");
        }
    }



    private IFileProcessor GetProcessor(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension) || !_processors.TryGetValue(extension, out var processor))
        {
            throw new FileProcessingException(
                "file_format_unsupported",
                "The uploaded file format is not supported.");
        }

        return processor;
    }



    private void RecordSuccess(Guid processingId, string fileName, long fileSizeBytes, FileProcessingOutcome outcome, long processingDurationMilliseconds)
    {
        _tracker.Record(
            new FileProcessingRecord(
                processingId,
                fileName,
                outcome.Format,
                fileSizeBytes,
                FileProcessingStatus.Succeeded,
                outcome.RecordsProcessed,
                processingDurationMilliseconds,
                DateTimeOffset.UtcNow,
                ErrorCode: null));
    }



    private void RecordFailure(Guid processingId, string fileName, string format, long fileSizeBytes, long processingDurationMilliseconds, string errorCode)
    {
        _tracker.Record(
            new FileProcessingRecord(
                processingId,
                GetTrackedFileName(fileName),
                format,
                fileSizeBytes,
                FileProcessingStatus.Failed,
                RecordsProcessed: null,
                processingDurationMilliseconds,
                DateTimeOffset.UtcNow,
                errorCode));
    }



    private static string SanitizeFileName(string submittedFileName)
    {
        var normalizedFileName = submittedFileName.Replace('\\', '/');
        return Path.GetFileName(normalizedFileName);
    }



    private static string GetTrackedFileName(string fileName) =>
        string.IsNullOrWhiteSpace(fileName)
            ? "unnamed"
            : fileName;


    private static string GetFormat(string extension)
    {
        var format = extension.TrimStart('.');
        return string.IsNullOrWhiteSpace(format)
            ? "unknown"
            : format.ToLowerInvariant();
    }

}
