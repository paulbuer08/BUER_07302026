using FileProcessingService.Api.Features.Files.Tracking;

using Microsoft.AspNetCore.Mvc;

namespace FileProcessingService.Api.Controllers;


[ApiController]
[Route("api/v1/files")]
[Produces("application/json")]
public sealed class FileProcessingReportsController : ControllerBase
{
    private readonly IFileProcessingTracker _tracker;

    public FileProcessingReportsController(IFileProcessingTracker tracker)
    {
        _tracker = tracker;
    }


    [HttpGet]
    [ProducesResponseType<FileProcessingReport>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<FileProcessingReport> GetReport()
    {
        return Ok(_tracker.GetReport());
    }
}
