using FileProcessingService.Api.Features.Files.Contracts;
using FileProcessingService.Api.Features.Files.Processing;
using FileProcessingService.Api.Features.Files.Services;

using Microsoft.AspNetCore.Mvc;

namespace FileProcessingService.Api.Controllers;


[ApiController]
[Route("api/v1/files")]
[Produces("application/json")]
public sealed partial class FilesController(IFileProcessingOrchestrator orchestrator, ILogger<FilesController> logger)
    : ControllerBase
{
    private readonly IFileProcessingOrchestrator _orchestrator = orchestrator;
    private readonly ILogger<FilesController> _logger = logger;


    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<FileUploadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<FileUploadResponse>> ProcessFileAsync(
        [FromForm] UploadFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _orchestrator.ProcessAsync(request.File, cancellationToken);
            LogFileProcessed(_logger, response.FileName, response.ProcessingId, response.ProcessingDurationMilliseconds);

            return Ok(response);
        }
        catch (FileProcessingException exception)
        {
            var statusCode = GetStatusCode(exception.ErrorCode);
            LogFileRejected(_logger, exception.ErrorCode);

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = "File processing failed",
                Detail = exception.Message,
                Instance = HttpContext.Request.Path
            };

            problem.Extensions["errorCode"] = exception.ErrorCode;
            return StatusCode(statusCode, problem);
        }
    }



    private static int GetStatusCode(string errorCode) =>
        errorCode switch
        {
            "file_too_large" => StatusCodes.Status413PayloadTooLarge,
            "file_format_unsupported" or "csv_support_disabled" or "json_support_disabled" => StatusCodes.Status415UnsupportedMediaType,
            _ => StatusCodes.Status400BadRequest
        };



    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message =
            "Processed file {FileName} with processing ID " +
            "{ProcessingId} in {ProcessingDurationMilliseconds} ms.")]
    private static partial void LogFileProcessed(
        ILogger logger,
        string fileName,
        Guid processingId,
        long processingDurationMilliseconds);



    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Rejected an uploaded file with error code {ErrorCode}.")]
    private static partial void LogFileRejected(
        ILogger logger,
        string errorCode);
}
