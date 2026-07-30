using FileProcessingService.Api.Configuration;

using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Features.Files.Tracking;


public sealed class InMemoryFileProcessingTracker(IOptions<FileProcessingOptions> options)
    : IFileProcessingTracker
{
    private readonly object _synchronizationLock = new();
    private readonly Queue<FileProcessingRecord> _recentFiles = new();
    private readonly int _historyLimit = options.Value.TrackingHistoryLimit;
    private long _totalFilesReceived;
    private long _totalFilesProcessedSuccessfully;
    private long _totalFilesFailed;

    public void Record(FileProcessingRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        lock (_synchronizationLock)
        {
            _totalFilesReceived++;
            UpdateStatusCounter(record.Status);
            _recentFiles.Enqueue(record);

            while (_recentFiles.Count > _historyLimit)
            {
                _recentFiles.Dequeue();
            }
        }
    }



    public FileProcessingReport GetReport()
    {
        lock (_synchronizationLock)
        {
            var recentFilesNewestFirst = _recentFiles.Reverse().ToArray();

            return new FileProcessingReport(
                _totalFilesReceived,
                _totalFilesProcessedSuccessfully,
                _totalFilesFailed,
                recentFilesNewestFirst);
        }
    }



    private void UpdateStatusCounter(FileProcessingStatus status)
    {
        switch (status)
        {
            case FileProcessingStatus.Succeeded:
                _totalFilesProcessedSuccessfully++;
                break;

            case FileProcessingStatus.Failed:
                _totalFilesFailed++;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "The file-processing status is unsupported.");
        }
    }

}
