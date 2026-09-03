using FileProcessingService.Api.Features.Files.Tracking;
using FileProcessingService.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileProcessingService.Api.Controllers;


[ApiController]
[Route("api/v1/files")]
[Produces("application/json")]
[Authorize(Policy = ApiKeyAuthorizationDefaults.ReadReportsPolicy)]
public sealed class FileProcessingReportsController(IFileProcessingTracker tracker)
    : ControllerBase
{
    private readonly IFileProcessingTracker _tracker = tracker;


    [HttpGet]
    [ProducesResponseType<FileProcessingReport>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public ActionResult<FileProcessingReport> GetReport()
    {
        return Ok(_tracker.GetReport());
    }
}
