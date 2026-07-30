using FileProcessingService.Api.Configuration;
using FileProcessingService.Api.Features.Files.Tracking;

using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Tests.Features.Files.Tracking;


public sealed class InMemoryFileProcessingTrackerTests
{
    [Fact]
    public void GetReportReturnsCountersAndNewestLimitedHistory()
    {
        var options = new FileProcessingOptions
        {
            TrackingHistoryLimit = 2
        };

        var tracker = new InMemoryFileProcessingTracker(Options.Create(options));
        tracker.Record(CreateRecord("first.csv", FileProcessingStatus.Succeeded, sequence: 1));
        tracker.Record(CreateRecord("second.csv", FileProcessingStatus.Failed, sequence: 2));
        tracker.Record(CreateRecord("third.csv", FileProcessingStatus.Succeeded, sequence: 3));

        var report = tracker.GetReport();

        Assert.Equal(3L, report.TotalFilesReceived);
        Assert.Equal(2L, report.TotalFilesProcessedSuccessfully);
        Assert.Equal(1L, report.TotalFilesFailed);

        Assert.Collection(
            report.RecentFiles,
            record => Assert.Equal("third.csv", record.FileName),
            record => Assert.Equal("second.csv", record.FileName));
    }



    private static FileProcessingRecord CreateRecord(string fileName, FileProcessingStatus status, int sequence)
    {
        var completedAtUtc = new DateTimeOffset(
            year: 2026,
            month: 7,
            day: 31,
            hour: 0,
            minute: 0,
            second: sequence,
            offset: TimeSpan.Zero);

        return new FileProcessingRecord(
            Guid.NewGuid(),
            fileName,
            "csv",
            FileSizeBytes: 100,
            status,
            RecordsProcessed: 1,
            ProcessingDurationMilliseconds: 10,
            completedAtUtc,
            ErrorCode: status == FileProcessingStatus.Failed ? "test_failure" : null);
    }
}
