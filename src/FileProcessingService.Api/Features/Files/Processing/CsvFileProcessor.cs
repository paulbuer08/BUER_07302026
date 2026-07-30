using System.Globalization;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

using FileProcessingService.Api.Configuration;

using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Features.Files.Processing;


public sealed class CsvFileProcessor : IFileProcessor
{
    private const string CsvFormat = "csv";
    private readonly FileProcessingOptions _options;

    public string SupportedFileExtension => ".csv";


    public CsvFileProcessor(IOptions<FileProcessingOptions> options)
    {
        _options = options.Value;
    }



    public async Task<FileProcessingOutcome> ProcessAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!_options.CsvSupport.IsEnabled)
        {
            throw new FileProcessingException(
                "csv_support_disabled",
                "CSV file processing is not enabled.");
        }

        var columnName = _options.CsvSupport.CalculateAverageInColumnName.Trim();
        try
        {
            return await ProcessCsvAsync(stream, columnName, _options.MaximumRecordCount, cancellationToken);
        }
        catch (FileProcessingException)
        {
            throw;
        }
        catch (DecoderFallbackException exception)
        {
            throw new FileProcessingException(
                "csv_encoding_invalid",
                "The CSV file must contain valid UTF-8 text.",
                exception);
        }
        catch (CsvHelperException exception)
        {
            throw new FileProcessingException(
                "csv_malformed",
                "The CSV content is malformed or cannot be read.",
                exception);
        }
    }



    private static async Task<FileProcessingOutcome> ProcessCsvAsync(Stream stream, string columnName, int maximumRecordCount, CancellationToken cancellationToken)
    {
        var csvConfiguration =
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim
            };

        var strictUtf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        using var reader = new StreamReader(stream, strictUtf8Encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        using var csv = new CsvReader(reader, csvConfiguration);

        var headers = await ReadHeadersAsync(csv, cancellationToken);
        var targetColumnIndex = FindTargetColumnIndex(headers, columnName);
        var (total, recordCount) = await ReadRecordsAsync(csv, targetColumnIndex, columnName, maximumRecordCount, cancellationToken);

        return new FileProcessingOutcome(
            CsvFormat,
            recordCount,
            new CsvAverageResult(
                columnName,
                total / recordCount));
    }



    private static async Task<string[]> ReadHeadersAsync(CsvReader csv, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await csv.ReadAsync())
        {
            throw new FileProcessingException("csv_empty", "The CSV file is empty.");
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord;

        if (headers is not { Length: > 0 })
        {
            throw new FileProcessingException(
                "csv_header_missing",
                "The CSV file must contain a header row.");
        }

        return headers;
    }



    private static async Task<(decimal Total, int RecordCount)> ReadRecordsAsync(CsvReader csv, int targetColumnIndex, string columnName, int maximumRecordCount, CancellationToken cancellationToken)
    {
        decimal total = 0;
        var recordCount = 0;

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            recordCount++;

            EnsureRecordLimit(recordCount, maximumRecordCount);
            var numericValue = ReadNumericValue(csv, targetColumnIndex, columnName, recordCount);
            total = AddToTotal(total, numericValue, columnName);
        }

        if (recordCount == 0)
        {
            throw new FileProcessingException(
                "csv_records_missing",
                "The CSV file does not contain any data records.");
        }

        return (total, recordCount);
    }



    private static void EnsureRecordLimit(int recordCount, int maximumRecordCount)
    {
        if (recordCount > maximumRecordCount)
        {
            throw new FileProcessingException(
                "csv_record_limit_exceeded",
                $"The CSV file exceeds the maximum of " +
                $"{maximumRecordCount} records.");
        }
    }



    private static decimal ReadNumericValue(CsvReader csv, int targetColumnIndex, string columnName, int recordNumber)
    {
        var rawValue = csv.GetField(targetColumnIndex);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new FileProcessingException(
                "csv_value_missing",
                $"Record {recordNumber} has no value in " +
                $"column '{columnName}'.");
        }
        if (!decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericValue))
        {
            throw new FileProcessingException(
                "csv_value_invalid",
                $"Record {recordNumber} contains a non-numeric " +
                $"value in column '{columnName}'.");
        }

        return numericValue;
    }



    private static decimal AddToTotal(decimal currentTotal, decimal numericValue, string columnName)
    {
        try
        {
            return checked(currentTotal + numericValue);
        }
        catch (OverflowException exception)
        {
            throw new FileProcessingException(
                "csv_total_out_of_range",
                $"The values in column '{columnName}' are too large " +
                $"to calculate safely.",
                exception);
        }
    }



    private static int FindTargetColumnIndex(string[] headers, string columnName)
    {
        var matchingIndex = -1;
        for (var index = 0; index < headers.Length; index++)
        {
            if (!string.Equals(headers[index].Trim(), columnName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (matchingIndex >= 0)
            {
                throw new FileProcessingException(
                    "csv_column_duplicated",
                    $"The CSV file contains more than one " +
                    $"'{columnName}' column.");
            }

            matchingIndex = index;
        }

        if (matchingIndex < 0)
        {
            throw new FileProcessingException(
                "csv_column_missing",
                $"The CSV file does not contain the required " +
                $"'{columnName}' column.");
        }

        return matchingIndex;
    }

}
