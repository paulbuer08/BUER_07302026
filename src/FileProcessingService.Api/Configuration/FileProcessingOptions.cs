namespace FileProcessingService.Api.Configuration;


public sealed class FileProcessingOptions
{
    public const string SectionName = "FileProcessing";
    public long MaximumFileSizeBytes { get; set; } = 5 * 1024 * 1024;
    public int MaximumRecordCount { get; set; } = 10_000;
    public int TrackingHistoryLimit { get; set; } = 100;
    public CsvSupportOptions CsvSupport { get; set; } = new();
    public JsonSupportOptions JsonSupport { get; set; } = new();
}


public sealed class CsvSupportOptions
{
    public bool IsEnabled { get; set; } = true;
    public string CalculateAverageInColumnName { get; set; } = "Amount";
}


public sealed class JsonSupportOptions
{
    public bool IsEnabled { get; set; }
    public bool? FilterIsActiveEquals { get; set; } = true;
    public decimal? FilterAmountGreaterThanOrEqualTo { get; set; } = 0;
    public decimal? FilterAmountLessThanOrEqualTo { get; set; }
}
