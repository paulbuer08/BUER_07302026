using System.Text;
using FileProcessingService.Api.Configuration;
using FileProcessingService.Api.Features.Files.Processing;
using Microsoft.Extensions.Options;

namespace FileProcessingService.Api.Tests.Features.Files.Processing;


public sealed class CsvFileProcessorTests
{
    [Fact]
    public async Task ProcessAsyncCalculatesConfiguredColumnAverage()
    {
        const string csv =
            """
            Id,Name,Amount
            1,Alpha,100.00
            2,Beta,200.00
            3,Gamma,300.00
            """;

        var processor = CreateProcessor();
        await using var stream = CreateStream(csv);

        var outcome = await processor.ProcessAsync(stream, CancellationToken.None);
        var result = Assert.IsType<CsvAverageResult>(outcome.Result);

        Assert.Equal("csv", outcome.Format);
        Assert.Equal(3, outcome.RecordsProcessed);
        Assert.Equal("Amount", result.ColumnName);
        Assert.Equal(200m, result.Average);
    }



    [Fact]
    public async Task ProcessAsyncRejectsMissingConfiguredColumn()
    {
        const string csv =
            """
            Id,Name,Price
            1,Alpha,100.00
            """;

        var processor = CreateProcessor();
        await using var stream = CreateStream(csv);

        var exception = await Assert.ThrowsAsync<FileProcessingException>(() => processor.ProcessAsync(stream, CancellationToken.None));
        Assert.Equal("csv_column_missing", exception.ErrorCode);
    }



    [Fact]
    public async Task ProcessAsyncRejectsNonNumericValues()
    {
        const string csv =
            """
            Id,Name,Amount
            1,Alpha,not-a-number
            """;

        var processor = CreateProcessor();
        await using var stream = CreateStream(csv);

        var exception = await Assert.ThrowsAsync<FileProcessingException>(() => processor.ProcessAsync(stream, CancellationToken.None));
        Assert.Equal("csv_value_invalid", exception.ErrorCode);
    }



    private static CsvFileProcessor CreateProcessor()
    {
        var options = new FileProcessingOptions
        {
            CsvSupport = new CsvSupportOptions
            {
                IsEnabled = true,
                CalculateAverageInColumnName = "Amount"
            }
        };

        return new CsvFileProcessor(Options.Create(options));
    }



    private static MemoryStream CreateStream(string content) =>
        new(Encoding.UTF8.GetBytes(content));
}
