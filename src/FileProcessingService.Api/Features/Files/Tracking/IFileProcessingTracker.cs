namespace FileProcessingService.Api.Features.Files.Tracking;


public interface IFileProcessingTracker
{
    void Record(FileProcessingRecord record);
    FileProcessingReport GetReport();
}
