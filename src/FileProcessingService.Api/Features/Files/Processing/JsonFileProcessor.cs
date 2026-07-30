using System.Text.Json;

using FileProcessingService.Api.Configuration;

using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Features.Files.Processing;


public sealed class JsonFileProcessor : IFileProcessor
{
    private const string JsonFormat = "json";
    private const string IsActivePropertyName = "isActive";
    private const string AmountPropertyName = "amount";
    private readonly FileProcessingOptions _options;

    public string SupportedFileExtension => ".json";

    public JsonFileProcessor(IOptions<FileProcessingOptions> options)
    {
        _options = options.Value;
    }


    public async Task<FileProcessingOutcome> ProcessAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!_options.JsonSupport.IsEnabled)
        {
            throw new FileProcessingException(
                "json_support_disabled",
                "JSON file processing is not enabled.");
        }
        try
        {
            return await ProcessJsonAsync(stream, _options.JsonSupport, _options.MaximumRecordCount, cancellationToken);
        }
        catch (FileProcessingException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new FileProcessingException(
                "json_malformed",
                "The JSON content is malformed or cannot be read.",
                exception);
        }
    }



    private static async Task<FileProcessingOutcome> ProcessJsonAsync(Stream stream, JsonSupportOptions options, int maximumRecordCount, CancellationToken cancellationToken)
    {
        var matchingRecords = new List<JsonElement>();
        var recordCount = 0;

        await foreach (var record in
            JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(stream, cancellationToken: cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            recordCount++;

            EnsureRecordLimit(recordCount, maximumRecordCount);
            EnsureRecordIsObject(record, recordCount);

            if (MatchesFilters(record, options, recordCount))
            {
                matchingRecords.Add(record.Clone());
            }
        }

        if (recordCount == 0)
        {
            throw new FileProcessingException(
                "json_records_missing",
                "The JSON file does not contain any records.");
        }

        return new FileProcessingOutcome(
            JsonFormat,
            recordCount,
            new JsonFilterResult(
                matchingRecords.Count,
                matchingRecords));
    }



    private static bool MatchesFilters(JsonElement record, JsonSupportOptions options, int recordNumber)
    {
        // Evaluate both independently so every enabled filter is validated.
        var matchesIsActive = MatchesIsActiveFilter(
            record,
            options.FilterIsActiveEquals,
            recordNumber);

        var matchesAmount = MatchesAmountFilter(
            record,
            options.FilterAmountGreaterThanOrEqualTo,
            options.FilterAmountLessThanOrEqualTo,
            recordNumber);

        return matchesIsActive && matchesAmount;
    }



    private static bool MatchesIsActiveFilter(JsonElement record, bool? expectedValue, int recordNumber)
    {
        if (expectedValue is null)
        {
            return true;
        }
        if (!record.TryGetProperty(IsActivePropertyName, out var isActiveElement))
        {
            throw new FileProcessingException(
                "json_is_active_missing",
                $"Record {recordNumber} does not contain the required " +
                $"'{IsActivePropertyName}' property.");
        }

        if (isActiveElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new FileProcessingException(
                "json_is_active_invalid",
                $"Record {recordNumber} contains an invalid " +
                $"'{IsActivePropertyName}' value.");
        }

        return isActiveElement.GetBoolean() == expectedValue.Value;
    }



    private static bool MatchesAmountFilter(JsonElement record, decimal? minimumAmount, decimal? maximumAmount, int recordNumber)
    {
        if (minimumAmount is null && maximumAmount is null)
        {
            return true;
        }
        if (!record.TryGetProperty(AmountPropertyName, out var amountElement))
        {
            throw new FileProcessingException(
                "json_amount_missing",
                $"Record {recordNumber} does not contain the required " +
                $"'{AmountPropertyName}' property.");
        }

        if (amountElement.ValueKind != JsonValueKind.Number || !amountElement.TryGetDecimal(out var amount))
        {
            throw new FileProcessingException(
                "json_amount_invalid",
                $"Record {recordNumber} contains an invalid " +
                $"'{AmountPropertyName}' value.");
        }

        var meetsMinimum = minimumAmount is null || amount >= minimumAmount.Value;
        var meetsMaximum = maximumAmount is null || amount <= maximumAmount.Value;

        return meetsMinimum && meetsMaximum;
    }



    private static void EnsureRecordLimit(int recordCount, int maximumRecordCount)
    {
        if (recordCount > maximumRecordCount)
        {
            throw new FileProcessingException(
                "json_record_limit_exceeded",
                $"The JSON file exceeds the maximum of " +
                $"{maximumRecordCount} records.");
        }
    }



    private static void EnsureRecordIsObject(JsonElement record, int recordNumber)
    {
        if (record.ValueKind != JsonValueKind.Object)
        {
            throw new FileProcessingException(
                "json_record_invalid",
                $"Record {recordNumber} must be a JSON object.");
        }
    }

}
