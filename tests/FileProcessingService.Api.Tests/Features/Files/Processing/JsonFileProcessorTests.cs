using System.Text;
using FileProcessingService.Api.Configuration;
using FileProcessingService.Api.Features.Files.Processing;
using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Tests.Features.Files.Processing;


public sealed class JsonFileProcessorTests
{
    [Fact]
    public async Task ProcessAsyncAppliesInclusiveAmountFilters()
    {
        const string json =
            """
            [
              { "id": 1, "isActive": true, "amount": 100 },
              { "id": 2, "isActive": false, "amount": 200 },
              { "id": 3, "isActive": true, "amount": 300 },
              { "id": 4, "isActive": true, "amount": 400 }
            ]
            """;

        var processor = CreateProcessor(
            filterIsActiveEquals: null,
            minimumAmount: 100,
            maximumAmount: 300);

        await using var stream = CreateStream(json);
        var outcome = await processor.ProcessAsync(stream, CancellationToken.None);
        var result = Assert.IsType<JsonFilterResult>(outcome.Result);

        Assert.Equal("json", outcome.Format);
        Assert.Equal(4, outcome.RecordsProcessed);
        Assert.Equal(3, result.RecordsMatched);

        Assert.Collection(
            result.Records,
            record => Assert.Equal(1, record.GetProperty("id").GetInt32()),
            record => Assert.Equal(2, record.GetProperty("id").GetInt32()),
            record => Assert.Equal(3, record.GetProperty("id").GetInt32()));
    }



    [Fact]
    public async Task ProcessAsyncReturnsInactiveRecordsWhenConfiguredFalse()
    {
        const string json =
            """
            [
              { "id": 1, "isActive": true },
              { "id": 2, "isActive": false },
              { "id": 3, "isActive": false }
            ]
            """;

        var processor = CreateProcessor(
            filterIsActiveEquals: false,
            minimumAmount: null,
            maximumAmount: null);

        await using var stream = CreateStream(json);
        var outcome = await processor.ProcessAsync(stream, CancellationToken.None);
        var result = Assert.IsType<JsonFilterResult>(outcome.Result);

        Assert.Equal(2, result.RecordsMatched);
        Assert.Collection(
            result.Records,
            record => Assert.Equal(2, record.GetProperty("id").GetInt32()),
            record => Assert.Equal(3, record.GetProperty("id").GetInt32()));
    }



    [Fact]
    public async Task ProcessAsyncReturnsAllRecordsWhenFiltersAreDisabled()
    {
        const string json =
            """
            [
              { "id": 1, "description": "No filter properties" },
              { "id": 2, "differentProperty": true }
            ]
            """;

        var processor = CreateProcessor(
            filterIsActiveEquals: null,
            minimumAmount: null,
            maximumAmount: null);

        await using var stream = CreateStream(json);
        var outcome = await processor.ProcessAsync(stream, CancellationToken.None);
        var result = Assert.IsType<JsonFilterResult>(outcome.Result);

        Assert.Equal(2, outcome.RecordsProcessed);
        Assert.Equal(2, result.RecordsMatched);
    }



    private static JsonFileProcessor CreateProcessor(bool? filterIsActiveEquals, decimal? minimumAmount, decimal? maximumAmount)
    {
        var options = new FileProcessingOptions
        {
            JsonSupport = new JsonSupportOptions
            {
                IsEnabled = true,
                FilterIsActiveEquals = filterIsActiveEquals,
                FilterAmountGreaterThanOrEqualTo = minimumAmount,
                FilterAmountLessThanOrEqualTo = maximumAmount
            }
        };

        return new JsonFileProcessor(Options.Create(options));
    }



    private static MemoryStream CreateStream(string content) =>
        new(Encoding.UTF8.GetBytes(content));
}
