using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Configuration;


public sealed class FileProcessingOptionsValidator
    : IValidateOptions<FileProcessingOptions>
{
    private const long AbsoluteMaximumFileSizeBytes = 50 * 1024 * 1024;
    private const int AbsoluteMaximumRecordCount = 100_000;
    private const int AbsoluteMaximumTrackingHistoryLimit = 1_000;


    public ValidateOptionsResult Validate(string? name, FileProcessingOptions options)
    {
        List<string> failures = [];
        if (options.MaximumFileSizeBytes is <= 0 or > AbsoluteMaximumFileSizeBytes)
        {
            failures.Add(
                "FileProcessing:MaximumFileSizeBytes must be between " +
                $"1 and {AbsoluteMaximumFileSizeBytes}.");
        }
        if (options.MaximumRecordCount is <= 0 or > AbsoluteMaximumRecordCount)
        {
            failures.Add(
                "FileProcessing:MaximumRecordCount must be between " +
                $"1 and {AbsoluteMaximumRecordCount}.");
        }
        if (options.TrackingHistoryLimit is <= 0 or > AbsoluteMaximumTrackingHistoryLimit)
        {
            failures.Add(
                "FileProcessing:TrackingHistoryLimit must be between " +
                $"1 and {AbsoluteMaximumTrackingHistoryLimit}.");
        }

        ValidateFormatSupport(options, failures);
        ValidateCsv(options.CsvSupport, failures);
        ValidateJson(options.JsonSupport, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }



    private static void ValidateFormatSupport(FileProcessingOptions options, List<string> failures)
    {
        if (!options.CsvSupport.IsEnabled && !options.JsonSupport.IsEnabled)
        {
            failures.Add(
                "At least one file format must be enabled under " +
                "FileProcessing:CsvSupport or FileProcessing:JsonSupport.");
        }
    }



    private static void ValidateCsv(CsvSupportOptions options, List<string> failures)
    {
        if (!options.IsEnabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.CalculateAverageInColumnName))
        {
            failures.Add(
                "FileProcessing:CsvSupport:" +
                "CalculateAverageInColumnName is required when CSV is enabled.");
        }
        else if (options.CalculateAverageInColumnName.Length > 128)
        {
            failures.Add(
                "FileProcessing:CsvSupport:" +
                "CalculateAverageInColumnName cannot exceed 128 characters.");
        }
    }



    private static void ValidateJson(JsonSupportOptions options, List<string> failures)
    {
        var minimum = options.FilterAmountGreaterThanOrEqualTo;
        var maximum = options.FilterAmountLessThanOrEqualTo;

        if (minimum.HasValue
            && maximum.HasValue
            && minimum.Value > maximum.Value)
        {
            failures.Add(
                "FileProcessing:JsonSupport:" +
                "FilterAmountGreaterThanOrEqualTo cannot exceed FilterAmountLessThanOrEqualTo.");
        }
    }

}
